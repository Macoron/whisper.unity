using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOT;
using UnityEngine;
using Whisper.Native;
using Whisper.Utils;
using Debug = UnityEngine.Debug;

namespace Whisper
{
    public class WhisperResult
    {
        public readonly List<string> Segments;
        public readonly string Result;

        public WhisperResult(List<string> segments)
        {
            Segments = segments;
            
            // generate full string based on segments
            var builder = new StringBuilder();
            foreach (var seg in segments)
            {
                builder.Append(seg);
            }
            Result = builder.ToString();
        }
    }

    public delegate void OnNewSegmentDelegate(int index, string text);
    
    public class WhisperWrapper
    {
        public const int WhisperSampleRate = 16000;

        public event OnNewSegmentDelegate OnNewSegment;

        private readonly IntPtr _whisperCtx;
        private readonly WhisperNativeParams _params;
        private readonly object _lock = new();

        private WhisperWrapper(IntPtr whisperCtx)
        {
            _whisperCtx = whisperCtx;
        }

        ~WhisperWrapper()
        {
            if (_whisperCtx == IntPtr.Zero)
                return;
            WhisperNative.whisper_free(_whisperCtx);
        }

        public WhisperResult GetText(AudioClip clip, WhisperParams param)
        {
            // try to load data
            var samples = new float[clip.samples * clip.channels];
            if (!clip.GetData(samples, 0))
            {
                Debug.LogError("Failed to load audio!");
                return null;
            }
            
            return GetText(samples, clip.frequency, clip.channels, param);
        }
        
        public async Task<WhisperResult> GetTextAsync(AudioClip clip, WhisperParams param)
        {
            var samples = new float[clip.samples * clip.channels];
            if (!clip.GetData(samples, 0))
            {
                Debug.LogError("Failed to load audio!");
                return null;
            }

            var frequency = clip.frequency;
            var channels = clip.channels;
            var asyncTask = Task.Factory.StartNew(() => GetText(samples, frequency, channels, param));
            return await asyncTask;
            
        }

        public WhisperResult GetText(float[] samples, int frequency, int channels, WhisperParams param)
        {
            lock (_lock)
            {
                // preprocess data if necessary
                Debug.Log("Preprocessing audio data...");
                var sw = new Stopwatch();
                sw.Start();
            
                var readySamples = AudioUtils.Preprocess(samples,frequency, channels, WhisperSampleRate);
            
                Debug.Log($"Audio data is preprocessed, total time: {sw.ElapsedMilliseconds} ms.");
            
                var gch = GCHandle.Alloc(this);
                var nativeParams = param.NativeParams;

                // add callback (if no custom callback set)
                if (nativeParams.new_segment_callback == null &&
                    nativeParams.new_segment_callback_user_data == IntPtr.Zero)
                {
                    nativeParams.new_segment_callback = NewSegmentCallbackStatic;
                    nativeParams.new_segment_callback_user_data = GCHandle.ToIntPtr(gch);
                }

                // start inference
                if (!InferenceWhisper(readySamples, nativeParams))
                    return null;
            
                gch.Free();

                Debug.Log("Trying to get number of text segments...");
                var n = WhisperNative.whisper_full_n_segments(_whisperCtx);
                Debug.Log($"Number of text segments: {n}");

                var list = new List<string>();
                for (var i = 0; i < n; ++i) {
                    Debug.Log($"Requesting text segment {i}...");
                    var textPtr = WhisperNative.whisper_full_get_segment_text(_whisperCtx, i);
                    var text = Marshal.PtrToStringAnsi(textPtr);
                    Debug.Log(text);

                    list.Add(text);
                }

                var res = new WhisperResult(list);
                Debug.Log($"Final text: {res.Result}");
                return res;
            }
        }

        public async Task<WhisperResult> GetTextAsync(float[] samples, int frequency, int channels, WhisperParams param)
        {
            var asyncTask = Task.Factory.StartNew(() => GetText(samples, frequency, channels, param));
            return await asyncTask;
        }

        private unsafe bool InferenceWhisper(float[] samples, WhisperNativeParams param)
        {
            Debug.Log("Inference Whisper on input data...");
                
            var sw = new Stopwatch();
            sw.Start();
            fixed (float* samplesPtr = samples)
            {
                var code = WhisperNative.whisper_full(_whisperCtx, param, samplesPtr, samples.Length);
                if (code != 0)
                {
                    Debug.LogError($"Whisper failed to process data! Error code: {code}.");
                    return false;
                }
            }

            Debug.Log($"Whisper inference finished, total time: {sw.ElapsedMilliseconds} ms.");
            return true;
        }

        [MonoPInvokeCallback(typeof(whisper_new_segment_callback))]
        private static void NewSegmentCallbackStatic(IntPtr ctx, int nNew, IntPtr userDataPtr)
        {
            // relay this static function to wrapper instance
            var wrapper = (WhisperWrapper) GCHandle.FromIntPtr(userDataPtr).Target;
            wrapper.NewSegmentCallback(nNew);
        }
        
        private void NewSegmentCallback(int nNew)
        {
            // start reading new segments
            var nSegments = WhisperNative.whisper_full_n_segments(_whisperCtx);
            var s0 = nSegments - nNew;
            for (var i = s0; i < nSegments; i++)
            {
                // raise event with new text segment
                var textPtr = WhisperNative.whisper_full_get_segment_text(_whisperCtx, i);
                var text = Marshal.PtrToStringAnsi(textPtr);
                OnNewSegment?.Invoke(i, text);
            }
        }
        
        public static async Task<WhisperWrapper> InitFromFileAsync(string modelPath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var buffer = await FileUtils.ReadFileAsync(modelPath);
            var res = await InitFromBufferAsync(buffer);
            return res;
#else
            var asyncTask = Task.Factory.StartNew(() => InitFromFile(modelPath));
            return await asyncTask;          
#endif
        }
        
        public static WhisperWrapper InitFromFile(string modelPath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var buffer = FileUtils.ReadFile(modelPath);
            var res = InitFromBuffer(buffer);
            return res;
#else
            // load model weights
            Debug.Log($"Trying to load Whisper model from {modelPath}...");
        
            // some sanity checks
            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError("Whisper model path is null or empty!");
                return null;
            }
            if (!File.Exists(modelPath))
            {
                Debug.LogError($"Whisper model path {modelPath} doesn't exist!");
                return null;
            }
        
            // actually loading model
            var sw = new Stopwatch();
            sw.Start();
            
            var ctx = WhisperNative.whisper_init_from_file(modelPath);
            if (ctx == IntPtr.Zero)
            {
                Debug.LogError("Failed to load Whisper model!");
                return null;
            }
            Debug.Log($"Whisper model is loaded, total time: {sw.ElapsedMilliseconds} ms.");
            
            return new WhisperWrapper(ctx);
#endif
        }

        public static WhisperWrapper InitFromBuffer(byte[] buffer)
        {
            Debug.Log($"Trying to load Whisper model from buffer...");
            if (buffer == null || buffer.Length == 0)
            {
                Debug.LogError("Whisper model buffer is null or empty!");
                return null;
            }
            
            // we need to write buffer length as size_t
            // UIntPtr will work because size_t is size of pointer
            var length = new UIntPtr((uint) buffer.Length);
            
            // actually loading model
            var sw = new Stopwatch();
            sw.Start();
            
            IntPtr ctx;
            unsafe
            {
                // this only works because whisper makes copy of the buffer
                fixed (byte* bufferPtr = buffer)
                {
                    ctx = WhisperNative.whisper_init_from_buffer((IntPtr) bufferPtr, length);
                }
            }
            
            if (ctx == IntPtr.Zero)
            {
                Debug.LogError("Failed to load Whisper model!");
                return null;
            }
            Debug.Log($"Whisper model is loaded, total time: {sw.ElapsedMilliseconds} ms.");
            
            return new WhisperWrapper(ctx);
        }
        
        public static async Task<WhisperWrapper> InitFromBufferAsync(byte[] buffer)
        {
            var asyncTask = Task.Factory.StartNew(() => InitFromBuffer(buffer));
            return await asyncTask;
        }
    }
}