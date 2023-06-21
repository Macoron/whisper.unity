using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Whisper.Native;

namespace Whisper
{
    /// <summary>
    /// Wrapper of native C++ whisper parameters.
    /// Use it to safely change inference parameters.
    /// </summary>
    public class WhisperParams
     {
         private WhisperNativeParams _param;
         private string _languageManaged;
         private IntPtr _languagePtr = IntPtr.Zero;
         private string _initialPromptManaged;
         private IntPtr _initialPromptPtr = IntPtr.Zero;
         
         /// <summary>
         /// Native C++ struct parameters.
         /// Do not change it in runtime directly, use setters.
         /// </summary>
         public WhisperNativeParams NativeParams => _param;
         
         private unsafe WhisperParams(WhisperNativeParams param)
         {
             _param = param;
     
             // copy language string to managed memory
             var languageStrPtr = new IntPtr(param.language);
             _languageManaged = Marshal.PtrToStringAnsi(languageStrPtr);
     
             // copy initial_prompt string to managed memory
             var initialPromptStrPtr = new IntPtr(param.initial_prompt);
             _initialPromptManaged = Marshal.PtrToStringAnsi(initialPromptStrPtr);
             
             // reset callbacks
             _param.new_segment_callback = null;
             _param.new_segment_callback_user_data = IntPtr.Zero;
         }
     
         ~WhisperParams()
         {
             FreeLanguageString();
             FreeInitialPromptString();
         }

         #region Basic Parameters
        
         /// <summary>
         /// Sampling Whisper strategy (greedy or beam search).
         /// </summary>
         public WhisperSamplingStrategy Strategy
         {
             get => _param.strategy;
             set => _param.strategy = value;
         }

         /// <summary>
         /// Count of threads (n_threads), should be >= 1.
         /// </summary>
         public int ThreadsCount
         {
             get => _param.n_threads;
             set
             {
                 if (value < 1)
                     throw new ArgumentException("Number of threads should be bigger or equal one.");
                 _param.n_threads = value;
             }
         }

         /// <summary>
         /// Max tokens to use from past text as prompt for the decoder.
         /// </summary>
         public int MaxTextContextCount
         {
             get => _param.n_max_text_ctx;
             set => _param.n_max_text_ctx = value;
         }

         /// <summary>
         /// Start audio offset in ms.
         /// </summary>
         public int OffsetMs
         {
             get => _param.offset_ms;
             set => _param.offset_ms = value;
         }

         /// <summary>
         /// Audio duration to process in ms.
         /// </summary>
         // TODO: doesn't work correctly for some reason
         public int DurationMs
         {
             get => _param.duration_ms;
             set => _param.duration_ms = value;
         }
         
         /// <summary>
         /// Translate from source language to English.
         /// </summary>
         /// <remarks>
         /// Generally improves English translation. 
         /// Override <see cref="Language"/> parameter. If you want to translate
         /// to another language, set this to false and use <see cref="Language"/> parameter.
         /// </remarks>
         public bool Translate
         {
             get => _param.translate;
             set => _param.translate = value;
         }

         /// <summary>
         /// Do not use past transcription (if any) as initial prompt for the decoder.
         /// </summary>
         public bool NoContext
         {
             get => _param.no_context;
             set => _param.no_context = value;
         }

         /// <summary>
         /// Force single segment output (useful for streaming).
         /// </summary>
         public bool SingleSegment
         {
             get => _param.single_segment;
             set => _param.single_segment = value;
         }
         
         /// <summary>
         /// Print special tokens (e.g. SOT, EOT, BEG, etc.)
         /// </summary>
         public bool PrintSpecial
         {
             get => _param.print_special;
             set => _param.print_special = value;
         }

         /// <summary>
         /// Print progress information in C++ log.
         /// It won't be shown in Unity console, but visible in Unity log file.
         /// <a href="https://docs.unity3d.com/Manual/LogFiles.html">Log file location.</a>
         /// </summary>
         public bool PrintProgress
         {
             get => _param.print_progress;
             set => _param.print_progress = value;
         }

         /// <summary>
         /// Print results from within whisper.cpp in C++ log (avoid it, use callback instead).
         /// It won't be shown in Unity console, but visible in Unity log file.
         /// <a href="https://docs.unity3d.com/Manual/LogFiles.html">Log file location.</a>
         /// </summary>
         public bool PrintRealtime
         {
             get => _param.print_realtime;
             set => _param.print_realtime = value;
         }
         
         /// <summary>
         /// Print timestamps for each text segment when printing realtime in C++ log.
         /// It won't be shown in Unity console, but visible in Unity log file.
         /// <a href="https://docs.unity3d.com/Manual/LogFiles.html">Log file location.</a>
         /// </summary>
         public bool PrintTimestamps
         {
             get => _param.print_timestamps;
             set => _param.print_timestamps = value;
         }
     
         /// <summary>
         /// Output text language code (ISO 639-1). For example "en", "es" or "de".
         /// For auto-detection, set to null, "" or "auto".
         /// </summary>
         /// <remarks>
         /// Input audio can be in any language. Whisper will try to translate your audio
         /// to selected language. If you want to translate into English use <see cref="Translate"/>
         /// for better quality.
         /// </remarks>
         public string Language
         {
             get => _languageManaged;
             set
             {
                 if (_languageManaged == value)
                     return;
                 
                 _languageManaged = value;
                 unsafe
                 {
                     // free previous string
                     FreeLanguageString();
                     
                     // copies string in unmanaged memory to avoid GC
                     if (_languageManaged == null) return;
                     _languagePtr = Marshal.StringToHGlobalAnsi(_languageManaged);
                     _param.language = (byte*)_languagePtr;
                 }
             }
         }

         /// <summary>
         /// initial prompt is converted to tokens and prepended to any existing text context from a previous call
         /// <a href="https://github.com/ggerganov/whisper.cpp/discussions/348#discussioncomment-4559682">Using example</a>
         /// </summary>
         public string InitialPrompt
         {
             get => _initialPromptManaged;
             set
             {
                 if (_initialPromptManaged == value)
                     return;
                 
                 _initialPromptManaged = value;
                 unsafe
                 {
                     // free previous string
                     FreeInitialPromptString();
                     
                     // copies string in unmanaged memory to avoid GC
                     if (_initialPromptManaged == null) return;
                     _initialPromptPtr = Marshal.StringToHGlobalAnsi(_initialPromptManaged);
                     _param.initial_prompt = (byte*)_initialPromptPtr;
                 }
             }
         }
         
         #endregion

         #region Tokens Parameters

         /// <summary>
         /// [EXPERIMENTAL] Try to estimate timestamps for each token in segment.
         /// </summary>
         public bool TokenTimestamps
         {
             get => _param.token_timestamps;
             set => _param.token_timestamps = value;
         }

         #endregion
         
         #region Speed Up

         /// <summary>
         /// [EXPERIMENTAL] Speed-up the audio by 2x using Phase Vocoder.
         /// These can significantly reduce the quality of the output.
         /// </summary>
         public bool SpeedUp
         {
             get => _param.speed_up;
             set => _param.speed_up = value;
         }

         /// <summary>
         /// [EXPERIMENTAL] Overwrite the audio context size (0 = use default).
         /// These can significantly reduce the quality of the output.
         /// </summary>
         public int AudioCtx
         {
             get => _param.audio_ctx;
             set => _param.audio_ctx = value;
         }

         #endregion

         #region Callbacks

         /// <summary>
         /// Called for every newly generated text segment.
         /// Because of IL2CPP, this should be a static function.
         /// <see cref="WhisperWrapper"/> and <see cref="WhisperManager"/> will use it
         /// to raise their custom event.
         /// </summary>
         public whisper_new_segment_callback NewSegmentCallback
         {
             get => _param.new_segment_callback;
             set => _param.new_segment_callback = value;
         }

         /// <summary>
         /// Pointer to data that you want to pass as a parameter for <see cref="NewSegmentCallback"/>.
         /// It will be relayed from whisper code without any changes.
         /// </summary>
         public IntPtr NewSegmentCallbackUserData
         {
             get => _param.new_segment_callback_user_data;
             set => _param.new_segment_callback_user_data = value;
         }
         
         /// <summary>
         /// Called on each progress update.
         /// Because of IL2CPP, this should be a static function.
         /// <see cref="WhisperWrapper"/> and <see cref="WhisperManager"/> will use it
         /// to raise their custom event.
         /// </summary>
         public whisper_progress_callback ProgressCallback
         {
             get => _param.progress_callback;
             set => _param.progress_callback = value;
         }
         
         /// <summary>
         /// Pointer to data that you want to pass as a parameter for <see cref="ProgressCallback"/>.
         /// It will be relayed from whisper code without any changes.
         /// </summary>
         public IntPtr ProgressCallbackUserData
         {
             get => _param.new_segment_callback_user_data;
             set => _param.new_segment_callback_user_data = value;
         }

         #endregion

         #region Unity Custom
         
         /// <summary>
         /// Output in <see cref="WhisperSegment"/> list of tokens.
         /// </summary>
         public bool EnableTokens { get; set; }

         #endregion
         
         private void FreeLanguageString()
         {
             // if C# allocated new string before - clear it
             // but only clear C# string, not C++ literals
             // this code assumes that whisper will not change language string in C++
             if (_languagePtr != IntPtr.Zero)
                 Marshal.FreeHGlobal(_languagePtr);
             _languagePtr = IntPtr.Zero;
         }
         
         private void FreeInitialPromptString()
         {
             // if C# allocated new string before - clear it
             // but only clear C# string, not C++ literals
             // this code assumes that whisper will not change initial prompt string in C++
             if (_initialPromptPtr != IntPtr.Zero)
                 Marshal.FreeHGlobal(_initialPromptPtr);
             _initialPromptPtr = IntPtr.Zero;
         }
         
         public static WhisperParams GetDefaultParams(WhisperSamplingStrategy strategy =
             WhisperSamplingStrategy.WHISPER_SAMPLING_GREEDY)
         {
             Debug.Log($"Requesting default Whisper params for strategy {strategy}...");
             var nativeParams = WhisperNative.whisper_full_default_params(strategy);
             Debug.Log("Default params generated!");

             var param = new WhisperParams(nativeParams)
             {
                 // usually don't need C++ output log in Unity
                 PrintProgress = false,
                 PrintRealtime = false,
                 PrintTimestamps = false
             };

             // for some reason on android one thread works
             // 10x faster than multithreading
#if UNITY_ANDROID && !UNITY_EDITOR
             param.ThreadsCount = 1;
#endif
             return param;
         }
     }   
}