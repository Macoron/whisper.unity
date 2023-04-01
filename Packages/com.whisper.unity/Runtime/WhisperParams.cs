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
     
         public string Language
         {
             get => _languageManaged;
             set
             {
                 if (string.IsNullOrEmpty(value))
                     throw new ArgumentException("Null or empty language strings are not allowed!");
                 
                 _languageManaged = value;
                 unsafe
                 {
                     // if C# allocated new string before - clear it
                     // but only clear C# string, not C++ literals
                     // this code assumes that whisper will not change language string in C++
                     if (_languagePtr != IntPtr.Zero)
                         Marshal.FreeHGlobal(_languagePtr);
                     
                     // copies string in unmanaged memory to avoid GC
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
             if (_languagePtr != IntPtr.Zero)
                 Marshal.FreeHGlobal(_languagePtr);
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