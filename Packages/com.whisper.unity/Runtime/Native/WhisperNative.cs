using System;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

using whisper_context_ptr = System.IntPtr;
using whisper_token = System.Int32;

namespace Whisper.Native
{
    /// <summary>
    /// Bindings to native whisper.cpp functions.
    /// </summary>
    public static unsafe class WhisperNative
    {
        
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        private const string LibraryName = "__Internal";
#else
        private const string LibraryName = "libwhisper";
#endif
        [DllImport(LibraryName)]
        public static extern whisper_context_ptr whisper_init_from_file(string path_model);
    
        [DllImport(LibraryName)]
        public static extern whisper_context_ptr whisper_init_from_buffer(IntPtr buffer, UIntPtr buffer_size);
        
        [DllImport(LibraryName)]
        public static extern int whisper_lang_max_id();
        
        [DllImport(LibraryName)]
        public static extern int whisper_lang_id(string lang);

        [DllImport(LibraryName)]
        public static extern IntPtr whisper_lang_str(int id);
        
        [DllImport(LibraryName)]
        public static extern whisper_token whisper_token_eot(whisper_context_ptr ctx);
    
        [DllImport(LibraryName)]
        public static extern WhisperNativeParams whisper_full_default_params(WhisperSamplingStrategy strategy);
    
        [DllImport(LibraryName)]
        public static extern int whisper_full(whisper_context_ptr ctx, WhisperNativeParams param, 
            float* samples, int n_samples);
    
        [DllImport(LibraryName)]
        public static extern int whisper_full_n_segments(whisper_context_ptr ctx);
        
        [DllImport(LibraryName)]
        public static extern int whisper_full_lang_id(whisper_context_ptr ctx);
        
        [DllImport(LibraryName)]
        public static extern int whisper_is_multilingual(whisper_context_ptr ctx);
        
        [DllImport(LibraryName)]
        public static extern ulong whisper_full_get_segment_t0(whisper_context_ptr ctx, int i_segment);
        
        [DllImport(LibraryName)]
        public static extern ulong whisper_full_get_segment_t1(whisper_context_ptr ctx, int i_segment);
    
        [DllImport(LibraryName)]
        public static extern IntPtr whisper_full_get_segment_text(whisper_context_ptr ctx, int i_segment);
        
        [DllImport(LibraryName)]
        public static extern int whisper_full_n_tokens(whisper_context_ptr ctx, int i_segment);
        
        [DllImport(LibraryName)]
        public static extern IntPtr whisper_full_get_token_text(whisper_context_ptr ctx, int i_segment, int i_token);
        
        [DllImport(LibraryName)]
        public static extern WhisperNativeTokenData whisper_full_get_token_data(whisper_context_ptr ctx, int i_segment, int i_token);
    
        [DllImport(LibraryName)]
        public static extern void whisper_free(whisper_context_ptr ctx);
    }
}

