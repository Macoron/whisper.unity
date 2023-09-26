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

namespace Whisper
{
    public delegate void OnNewSegmentDelegate(WhisperSegment text);
    public delegate void OnProgressDelegate(int progress);

    /// <summary>
    /// Wrapper for loaded whisper model.
    /// </summary>
    public class WhisperWrapper
    {
        public const int WhisperSampleRate = 16000;

        /// <summary>
        /// Raised when whisper transcribed a new text segment from audio. 
        /// </summary>
        /// <remarks>Use <see cref="MainThreadDispatcher"/> for handling event in Unity main thread.</remarks>
        public event OnNewSegmentDelegate OnNewSegment;
        
        /// <summary>
        /// Raised when whisper made some progress in transcribing audio.
        /// Progress changes from 0 to 100 included.
        /// </summary>
        /// <remarks>Use <see cref="MainThreadDispatcher"/> for handling event in Unity main thread.</remarks>
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

        /// <summary>
        /// Checks if currently loaded whisper model supports multilingual transcription.
        /// </summary>
        public bool IsMultilingual => WhisperNative.whisper_is_multilingual(_whisperCtx) != 0;

        /// <summary>
        /// Transcribes audio clip. Will block thread until transcription complete.
        /// </summary>
        /// <returns>Full audio transcript. Null if transcription failed.</returns>
        public WhisperResult GetText(AudioClip clip, WhisperParams param)
        {
            // try to load data
            var samples = new float[clip.samples * clip.channels];
            if (!clip.GetData(samples, 0))
            {
                LogUtils.Error($"Failed to get audio data from clip {clip.name}!");
                return null;
            }
            
            return GetText(samples, clip.frequency, clip.channels, param);
        }
        
        /// <summary>
        /// Start async transcription of audio clip.
        /// </summary>
        /// <returns>Full audio transcript. Null if transcription failed.</returns>
        public async Task<WhisperResult> GetTextAsync(AudioClip clip, WhisperParams param)
        {
            var samples = new float[clip.samples * clip.channels];
            if (!clip.GetData(samples, 0))
            {
                LogUtils.Error($"Failed to get audio data from clip {clip.name}!");
                return null;
            }

            var frequency = clip.frequency;
            var channels = clip.channels;
            var asyncTask = Task.Factory.StartNew(() => GetText(samples, frequency, channels, param));
            return await asyncTask;
            
        }

        /// <summary>
        /// Transcribe audio buffer. Will block thread until transcription complete.
        /// </summary>
        /// <param name="samples">Raw audio buffer.</param>
        /// <param name="frequency">Audio sample rate.</param>
        /// <param name="channels">Audio channels count.</param>
        /// <param name="param">Whisper inference parameters.</param>
        /// <returns>Full audio transcript. Null if transcription failed.</returns>
        public WhisperResult GetText(float[] samples, int frequency, int channels, WhisperParams param)
        {
            lock (_lock)
            {
                // preprocess data if necessary
                LogUtils.Verbose("Preprocessing audio data...");
                var sw = new Stopwatch();
                sw.Start();
            
                var readySamples = AudioUtils.Preprocess(samples,frequency, channels, WhisperSampleRate);
            
                LogUtils.Verbose($"Audio data is preprocessed, total time: {sw.ElapsedMilliseconds} ms.");

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

                LogUtils.Verbose("Trying to get number of text segments...");
                var n = WhisperNative.whisper_full_n_segments(_whisperCtx);
                LogUtils.Verbose($"Number of text segments: {n}");

                var list = new List<WhisperSegment>();
                for (var i = 0; i < n; ++i)
                {
                    var segment = GetSegment(i, param);
                    list.Add(segment);
                }

                var langId = WhisperNative.whisper_full_lang_id(_whisperCtx);
                var res = new WhisperResult(list, langId);
                LogUtils.Log($"Final text: {res.Result}");
                return res;
            }
        }

        /// <summary>
        /// Start async transcription of audio buffer.
        /// </summary>
        /// <param name="samples">Raw audio buffer.</param>
        /// <param name="frequency">Audio sample rate.</param>
        /// <param name="channels">Audio channels count.</param>
        /// <param name="param">Whisper inference parameters.</param>
        /// <returns>Full audio transcript. Null if transcription failed.</returns>
        public async Task<WhisperResult> GetTextAsync(float[] samples, int frequency, int channels, WhisperParams param)
        {
            var asyncTask = Task.Factory.StartNew(() => GetText(samples, frequency, channels, param));
            return await asyncTask;
        }

        private unsafe bool InferenceWhisper(float[] samples, WhisperNativeParams param)
        {
            LogUtils.Log("Inference Whisper on input data...");
                
            var sw = new Stopwatch();
            sw.Start();
            fixed (float* samplesPtr = samples)
            {
                var code = WhisperNative.whisper_full(_whisperCtx, param, samplesPtr, samples.Length);
                if (code != 0)
                {
                    LogUtils.Error($"Whisper failed to process data! Error code: {code}.");
                    return false;
                }
            }

            LogUtils.Log($"Whisper inference finished, total time: {sw.ElapsedMilliseconds} ms.");
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
        private static void ProgressCallbackStatic(IntPtr ctx, IntPtr state, int progress, IntPtr userDataPtr)
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
        
        /// <summary>
        /// Loads whisper model from file path.
        /// </summary>
        /// <param name="modelPath">Absolute file path to model weights.</param>
        /// <returns>Loaded whisper model. Null if loading failed.</returns>
        public static WhisperWrapper InitFromFile(string modelPath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var buffer = FileUtils.ReadFile(modelPath);
            var res = InitFromBuffer(buffer);
            return res;
#else
            // load model weights
            LogUtils.Log($"Trying to load Whisper model from {modelPath}...");
        
            // some sanity checks
            if (string.IsNullOrEmpty(modelPath))
            {
                LogUtils.Error("Whisper model path is null or empty!");
                return null;
            }
            if (!File.Exists(modelPath))
            {
                LogUtils.Error($"Whisper model path {modelPath} doesn't exist!");
                return null;
            }
        
            // actually loading model
            var sw = new Stopwatch();
            sw.Start();
            
            var ctx = WhisperNative.whisper_init_from_file(modelPath);
            if (ctx == IntPtr.Zero)
            {
                LogUtils.Error("Failed to load Whisper model!");
                return null;
            }
            LogUtils.Log($"Whisper model is loaded, total time: {sw.ElapsedMilliseconds} ms.");
            
            return new WhisperWrapper(ctx);
#endif
        }

        /// <summary>
        /// Start async loading of whisper model from file path.
        /// </summary>
        /// <param name="modelPath">Absolute file path to model weights.</param>
        /// <returns>Loaded whisper model. Null if loading failed.</returns>
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
        
        /// <summary>
        /// Loads whisper model from byte buffer.
        /// </summary>
        /// <returns>Loaded whisper model. Null if loading failed.</returns>
        public static WhisperWrapper InitFromBuffer(byte[] buffer)
        {
            LogUtils.Log($"Trying to load Whisper model from buffer...");
            if (buffer == null || buffer.Length == 0)
            {
                LogUtils.Error("Whisper model buffer is null or empty!");
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
                LogUtils.Error("Failed to load Whisper model!");
                return null;
            }
            LogUtils.Log($"Whisper model is loaded, total time: {sw.ElapsedMilliseconds} ms.");
            
            return new WhisperWrapper(ctx);
        }
        
        /// <summary>
        /// Start async loading of whisper model from byte buffer.
        /// </summary>
        /// <returns>Loaded whisper model. Null if loading failed.</returns>
        public static async Task<WhisperWrapper> InitFromBufferAsync(byte[] buffer)
        {
            var asyncTask = Task.Factory.StartNew(() => InitFromBuffer(buffer));
            return await asyncTask;
        }
        
        private struct WhisperUserData
        {
            public readonly WhisperWrapper Wrapper;
            public readonly WhisperParams Param;
            
            public WhisperUserData(WhisperWrapper wrapper, WhisperParams param)
            {
                Wrapper = wrapper;
                Param = param;
            }
        }
    }
}