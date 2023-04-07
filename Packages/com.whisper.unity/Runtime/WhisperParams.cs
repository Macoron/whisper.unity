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
         // TODO: doesn't work correctly
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