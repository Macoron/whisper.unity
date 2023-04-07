using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Whisper.Native;

namespace Whisper
{
    public class WhisperParams
     {
         private WhisperNativeParams _param;
         private string _languageManaged;
         private IntPtr _languagePtr = IntPtr.Zero;
         
         private unsafe WhisperParams(WhisperNativeParams param)
         {
             _param = param;
     
             // copy language string to managed memory
             var strPtr = new IntPtr(param.language);
             _languageManaged = Marshal.PtrToStringAnsi(strPtr);
         }
     
         ~WhisperParams()
         {
             FreeLanguageString();
         }
         
        #region Parameters
        
        /// <summary>
        /// Native C++ struct parameters. To change use setters.
        /// </summary>
         public WhisperNativeParams NativeParams => _param;
         
         /// <summary>
         /// Sampling Whisper strategy.
         /// Greedy tends to be faster, but has lower quality.
         /// Beam search slower, but higher quality.
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
         public string Language
         {
             get => _languageManaged;
             set
             {
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
         
         public static WhisperParams GetDefaultParams(WhisperSamplingStrategy strategy =
             WhisperSamplingStrategy.WHISPER_SAMPLING_GREEDY)
         {
             Debug.Log($"Requesting default Whisper params for strategy {strategy}...");
             var param = WhisperNative.whisper_full_default_params(strategy);
             param.no_context = true;
             Debug.Log("Default params generated!");
                 
             return new WhisperParams(param);
         }
     }   
}