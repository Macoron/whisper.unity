using System;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

using whisper_token_ptr = System.IntPtr;
using whisper_context_ptr = System.IntPtr;
using whisper_state_ptr = System.IntPtr;
using whisper_token = System.Int32;


namespace Whisper.Native
{
    public enum WhisperSamplingStrategy
    {
        WHISPER_SAMPLING_GREEDY = 0, // similar to OpenAI's GreefyDecoder
        WHISPER_SAMPLING_BEAM_SEARCH = 1, // similar to OpenAI's BeamSearchDecoder
    };

    enum WhisperAlignmentHeadsPreset
    {
        WHISPER_AHEADS_NONE,
        WHISPER_AHEADS_N_TOP_MOST,  // All heads from the N-top-most text-layers
        WHISPER_AHEADS_CUSTOM,
        WHISPER_AHEADS_TINY_EN,
        WHISPER_AHEADS_TINY,
        WHISPER_AHEADS_BASE_EN,
        WHISPER_AHEADS_BASE,
        WHISPER_AHEADS_SMALL_EN,
        WHISPER_AHEADS_SMALL,
        WHISPER_AHEADS_MEDIUM_EN,
        WHISPER_AHEADS_MEDIUM,
        WHISPER_AHEADS_LARGE_V1,
        WHISPER_AHEADS_LARGE_V2,
        WHISPER_AHEADS_LARGE_V3,
        WHISPER_AHEADS_LARGE_V3_TURBO,
    };

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void whisper_new_segment_callback(whisper_context_ptr ctx, whisper_state_ptr state,
        int n_new, IntPtr user_data);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void whisper_progress_callback(whisper_context_ptr ctx, whisper_state_ptr state,
        int progress, IntPtr user_data);
    
    /// <summary>
    /// This is direct copy of C++ struct.
    /// Do not change or add any fields without changing it in whisper.cpp.
    /// Check <see cref="WhisperTokenData"/> for more information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WhisperNativeTokenData 
    {
        public whisper_token id;  // token id
        public whisper_token tid; // forced timestamp token id

        public float p;           // probability of the token
        public float plog;        // log probability of the token
        public float pt;          // probability of the timestamp token
        public float ptsum;       // sum of probabilities of all timestamp tokens

        // token-level timestamp data
        // do not use if you haven't computed token-level timestamps
        public ulong t0;        // start time of the token
        public ulong t1;        //   end time of the token

        // [EXPERIMENTAL] Token-level timestamps with DTW
        // do not use if you haven't computed token-level timestamps with dtw
        // Roughly corresponds to the moment in audio in which the token was output
        ulong t_dtw;

        public float vlen;        // voice length of the token
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WhisperNativeAheads
    {
        UIntPtr n_heads;
        IntPtr heads;
    }

    /// <summary>
    /// This is direct copy of C++ struct.
    /// Do not change or add any fields without changing it in whisper.cpp.
    /// Do not change it in runtime directly, use <see cref="WhisperContextParams"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WhisperNativeContextParams
    {
        [MarshalAs(UnmanagedType.U1)] public bool use_gpu;
        [MarshalAs(UnmanagedType.U1)] public bool flash_attn;
        int gpu_device;  // CUDA device

        // [EXPERIMENTAL] Token-level timestamps with DTW
        [MarshalAs(UnmanagedType.U1)] bool dtw_token_timestamps;
        WhisperAlignmentHeadsPreset dtw_aheads_preset;

        int dtw_n_top;
        WhisperNativeAheads dtw_aheads;

        UIntPtr dtw_mem_size; // TODO: remove
    };

    /// <summary>
    /// This is direct copy of C++ struct.
    /// Do not change or add any fields without changing it in whisper.cpp.
    /// Do not change it in runtime directly, use <see cref="WhisperParams"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct WhisperNativeParams
    {
        public WhisperSamplingStrategy strategy;

        public int n_threads;
        public int n_max_text_ctx; // max tokens to use from past text as prompt for the decoder
        public int offset_ms; // start offset in ms
        public int duration_ms; // audio duration to process in ms

        [MarshalAs(UnmanagedType.U1)] public bool translate;
        [MarshalAs(UnmanagedType.U1)] public bool no_context; // do not use past transcription (if any) as initial prompt for the decoder
        [MarshalAs(UnmanagedType.U1)] bool no_timestamps;     // do not generate timestamps
        [MarshalAs(UnmanagedType.U1)] public bool single_segment; // force single segment output (useful for streaming)
        [MarshalAs(UnmanagedType.U1)] public bool print_special; // print special tokens (e.g. <SOT>, <EOT>, <BEG>, etc.)
        [MarshalAs(UnmanagedType.U1)] public bool print_progress; // print progress information
        [MarshalAs(UnmanagedType.U1)] public bool print_realtime; // print results from within whisper.cpp (avoid it, use callback instead)
        [MarshalAs(UnmanagedType.U1)] public bool print_timestamps; // print timestamps for each text segment when printing realtime

        // [EXPERIMENTAL] token-level timestamps
        [MarshalAs(UnmanagedType.U1)] public bool token_timestamps; // enable token-level timestamps
        float thold_pt; // timestamp token probability threshold (~0.01)
        float thold_ptsum; // timestamp token sum probability threshold (~0.01)
        int max_len; // max segment length in characters
        [MarshalAs(UnmanagedType.U1)] bool split_on_word; // split on word rather than on token (when used with max_len)
        int max_tokens; // max tokens per segment (0 = no limit)

        // [EXPERIMENTAL] speed-up techniques
        // note: these can significantly reduce the quality of the output
        [MarshalAs(UnmanagedType.U1)] bool debug_mode;        // enable debug_mode provides extra info (eg. Dump log_mel)
        public int audio_ctx; // overwrite the audio context size (0 = use default)

        // [EXPERIMENTAL] [TDRZ] tinydiarize
        [MarshalAs(UnmanagedType.U1)] bool tdrz_enable;       // enable tinydiarize speaker turn detection

        // A regular expression that matches tokens to suppress
        byte* suppress_regex;

        // tokens to provide to the whisper decoder as initial prompt
        // these are prepended to any existing text context from a previous call
        // use whisper_tokenize() to convert text to tokens
        // maximum of whisper_n_text_ctx()/2 tokens are used (typically 224)
        public byte* initial_prompt;
        whisper_token_ptr prompt_tokens;
        int prompt_n_tokens;

        // for auto-detection, set to nullptr, "" or "auto"
        public byte* language;
        [MarshalAs(UnmanagedType.U1)] bool detect_language;

        // common decoding parameters:
        [MarshalAs(UnmanagedType.U1)] bool suppress_blank; // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/decoding.py#L89
        [MarshalAs(UnmanagedType.U1)] bool suppress_non_speech_tokens; // ref: https://github.com/openai/whisper/blob/7858aa9c08d98f75575035ecd6481f462d66ca27/whisper/tokenizer.py#L224-L253

        float temperature; // initial decoding temperature, ref: https://ai.stackexchange.com/a/32478
        float max_initial_ts; // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/decoding.py#L97
        float length_penalty; // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/transcribe.py#L267

        // fallback parameters
        // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/transcribe.py#L274-L278
        float temperature_inc;
        float entropy_thold; // similar to OpenAI's "compression_ratio_threshold"
        float logprob_thold;
        float no_speech_thold; // TODO: not implemented

        [StructLayout(LayoutKind.Sequential)]
        struct greedy_struct
        {
            int best_of; // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/transcribe.py#L264
        }

        greedy_struct greedy;

        [StructLayout(LayoutKind.Sequential)]
        struct beam_search_struct
        {
            int beam_size; // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/transcribe.py#L265
            float patience; // TODO: not implemented, ref: https://arxiv.org/pdf/2204.05424.pdf
        }

        beam_search_struct beam_search;

        // called for every newly generated text segment
        public whisper_new_segment_callback new_segment_callback;
        public IntPtr new_segment_callback_user_data;

        // called on each progress update
        public whisper_progress_callback progress_callback;
        public IntPtr progress_callback_user_data;

        // called each time before the encoder starts
        void* encoder_begin_callback;
        void* encoder_begin_callback_user_data;

        // called each time before ggml computation starts
        void* abort_callback;
        void* abort_callback_user_data;

        // called by each decoder to filter obtained logits
        void* logits_filter_callback;
        void* logits_filter_callback_user_data;

        IntPtr grammar_rules;
        UIntPtr n_grammar_rules;
        UIntPtr i_start_rule;
        float grammar_penalty;
    }
}
