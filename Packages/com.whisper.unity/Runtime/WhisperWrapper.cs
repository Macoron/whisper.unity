using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AOT;
using UnityEngine;
using Whisper.Native;
using Whisper.Utils;
using Debug = UnityEngine.Debug;

namespace Whisper
{
    public delegate void OnNewSegmentDelegate(WhisperSegment text);
    public delegate void OnProgressDelegate(int progress);
    
    public class WhisperWrapper
    {
        public const int WhisperSampleRate = 16000;

        public event OnNewSegmentDelegate OnNewSegment;
        public event OnProgressDelegate OnProgress;

        private readonly IntPtr _whisperCtx;
        private readonly WhisperNativeParams _params;
        private readonly object _lock = new object();

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

        public bool IsMultilingual => WhisperNative.whisper_is_multilingual(_whisperCtx) != 0;

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

                var userData = new WhisperUserData(this, param);
                var gch = GCHandle.Alloc(userData);
                var nativeParams = param.NativeParams;
                
                // add callback (if no custom callback set)
                if (nativeParams.new_segment_callback == null &&
                    nativeParams.new_segment_callback_user_data == IntPtr.Zero)
                {
                    nativeParams.new_segment_callback = NewSegmentCallbackStatic;
                    nativeParams.new_segment_callback_user_data = GCHandle.ToIntPtr(gch);
                }
                
                if (nativeParams.progress_callback == null &&
                    nativeParams.progress_callback_user_data == IntPtr.Zero)
                {
                    nativeParams.progress_callback = ProgressCallbackStatic;
                    nativeParams.progress_callback_user_data = GCHandle.ToIntPtr(gch);
                }

                // start inference
                if (!InferenceWhisper(readySamples, nativeParams))
                    return null;
            
                gch.Free();

                Debug.Log("Trying to get number of text segments...");
                var n = WhisperNative.whisper_full_n_segments(_whisperCtx);
                Debug.Log($"Number of text segments: {n}");

                var list = new List<WhisperSegment>();
                for (var i = 0; i < n; ++i)
                {
                    var segment = GetSegment(i, param);
                    list.Add(segment);
                }

                var langId = WhisperNative.whisper_full_lang_id(_whisperCtx);
                var res = new WhisperResult(list, langId);
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
        private static void NewSegmentCallbackStatic(IntPtr ctx, IntPtr state, int nNew, IntPtr userDataPtr)
        {
            // relay this static function to wrapper instance
            var userData = (WhisperUserData) GCHandle.FromIntPtr(userDataPtr).Target;
            userData.Wrapper.NewSegmentCallback(nNew, userData.Param);
        }
        
        private void NewSegmentCallback(int nNew, WhisperParams param)
        {
            // start reading new segments
            var nSegments = WhisperNative.whisper_full_n_segments(_whisperCtx);
            var s0 = nSegments - nNew;
            for (var i = s0; i < nSegments; i++)
            {
                var segment = GetSegment(i, param);
                OnNewSegment?.Invoke(segment);
            }
        }
        
        [MonoPInvokeCallback(typeof(whisper_progress_callback))]
        private void ProgressCallbackStatic(IntPtr ctx, IntPtr state, int progress, IntPtr userDataPtr)
        {
            // relay this static function to wrapper instance
            var userData = (WhisperUserData) GCHandle.FromIntPtr(userDataPtr).Target;
            userData.Wrapper.ProgressCallback(progress);
        }

        private void ProgressCallback(int progress)
        {
            OnProgress?.Invoke(progress);
        }

        private WhisperSegment GetSegment(int i, WhisperParams param)
        {
            // get segment text and timestamps
            var textPtr = WhisperNative.whisper_full_get_segment_text(_whisperCtx, i);
            var text = TextUtils.StringFromNativeUtf8(textPtr);
            var start = WhisperNative.whisper_full_get_segment_t0(_whisperCtx, i);
            var end = WhisperNative.whisper_full_get_segment_t1(_whisperCtx, i);
            var segment = new WhisperSegment(i, text, start, end);

            // return earlier if tokens are disabled
            if (!param.EnableTokens)
                return segment;
            
            // get all tokens
            var tokensN = WhisperNative.whisper_full_n_tokens(_whisperCtx, i);
            segment.Tokens = new WhisperTokenData[tokensN];
            for (var j = 0; j < tokensN; j++)
            {
                var nativeToken = WhisperNative.whisper_full_get_token_data(_whisperCtx, i, j);
                var textTokenPtr = WhisperNative.whisper_full_get_token_text(_whisperCtx, i, j);
                var textToken = TextUtils.StringFromNativeUtf8(textTokenPtr);
                var isSpecial = nativeToken.id >= WhisperNative.whisper_token_eot(_whisperCtx); 
                var token = new WhisperTokenData(nativeToken, textToken, param.TokenTimestamps, isSpecial);
                segment.Tokens[j] = token;
            }

            return segment;
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
        
        private struct WhisperUserData
        {
            public WhisperWrapper Wrapper;
            public WhisperParams Param;
            
            public WhisperUserData(WhisperWrapper wrapper, WhisperParams param)
            {
                Wrapper = wrapper;
                Param = param;
            }
        }
    }
}