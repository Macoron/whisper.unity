using System;
using System.Runtime.InteropServices;
using UnityEngine;
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
#if (UNITY_IOS || UNITY_VISIONOS || UNITY_ANDROID) && !UNITY_EDITOR
        private const string LibraryName = "__Internal";
#else
        private const string LibraryName = "libwhisper";
#endif

// this hack is needed for manual loading of dylib dependencies
// basicly unity doesn't support chain deps, so I need to manually
// load all dylibs in the right order
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        [DllImport("libdl.dylib")]
        private static extern IntPtr dlopen(string path, int mode);

        private const int RTLD_NOW = 2;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadNativeLibraries()
        {
#if UNITY_EDITOR_OSX
            var folder = "Packages/com.whisper.unity/Plugins/MacOS/";
#elif UNITY_STANDALONE_OSX
            var folder = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Plugins/");
#endif
            dlopen(folder + "libggml-base.dylib", RTLD_NOW);
            dlopen(folder + "libggml-blas.dylib", RTLD_NOW);
            dlopen(folder + "libggml-cpu.dylib", RTLD_NOW);
            dlopen(folder + "libggml-metal.dylib", RTLD_NOW);
            dlopen(folder + "libggml.dylib", RTLD_NOW);
        }
#endif

        
        [DllImport(LibraryName)]
        public static extern whisper_context_ptr whisper_init_from_buffer_with_params(IntPtr buffer,
            UIntPtr buffer_size, WhisperNativeContextParams @params);

        [DllImport(LibraryName)]
        public static extern int whisper_lang_max_id();

        [DllImport(LibraryName)]
        public static extern int whisper_lang_id(string lang);

        [DllImport(LibraryName)]
        public static extern IntPtr whisper_lang_str(int id);

        [DllImport(LibraryName)]
        public static extern whisper_token whisper_token_eot(whisper_context_ptr ctx);

        [DllImport(LibraryName)]
        public static extern IntPtr whisper_print_system_info();

        [DllImport(LibraryName)]
        public static extern WhisperNativeParams whisper_full_default_params(WhisperSamplingStrategy strategy);

        [DllImport(LibraryName)]
        public static extern WhisperNativeContextParams whisper_context_default_params();

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
        public static extern WhisperNativeTokenData whisper_full_get_token_data(whisper_context_ptr ctx, int i_segment,
            int i_token);

        [DllImport(LibraryName)]
        public static extern void whisper_free(whisper_context_ptr ctx);
    }
}