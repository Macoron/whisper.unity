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