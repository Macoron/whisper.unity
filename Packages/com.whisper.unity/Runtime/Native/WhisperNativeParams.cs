using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

using whisper_token_ptr = System.IntPtr;

namespace Whisper.Native
{
    public enum WhisperSamplingStrategy
    {
        WHISPER_SAMPLING_GREEDY = 0, // similar to OpenAI's GreefyDecoder
        WHISPER_SAMPLING_BEAM_SEARCH = 1, // similar to OpenAI's BeamSearchDecoder
    };

    // This is direct copy of C++ struct
    // Do not change or add any fields without changing it in whisper.cpp
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct WhisperNativeParams
    {
        public WhisperSamplingStrategy strategy;

        public int n_threads;
        public int n_max_text_ctx; // max tokens to use from past text as prompt for the decoder
        public int offset_ms; // start offset in ms
        public int duration_ms; // audio duration to process in ms

        [MarshalAs(UnmanagedType.U1)] 
        public bool translate;

        [MarshalAs(UnmanagedType.U1)]
        public bool no_context; // do not use past transcription (if any) as initial prompt for the decoder

        [MarshalAs(UnmanagedType.U1)] bool single_segment; // force single segment output (useful for streaming)
        [MarshalAs(UnmanagedType.U1)] bool print_special; // print special tokens (e.g. <SOT>, <EOT>, <BEG>, etc.)
        [MarshalAs(UnmanagedType.U1)] bool print_progress; // print progress information

        [MarshalAs(UnmanagedType.U1)]
        bool print_realtime; // print results from within whisper.cpp (avoid it, use callback instead)

        [MarshalAs(UnmanagedType.U1)]
        bool print_timestamps; // print timestamps for each text segment when printing realtime

        // [EXPERIMENTAL] token-level timestamps
        [MarshalAs(UnmanagedType.U1)] bool token_timestamps; // enable token-level timestamps
        float thold_pt; // timestamp token probability threshold (~0.01)
        float thold_ptsum; // timestamp token sum probability threshold (~0.01)
        int max_len; // max segment length in characters
        [MarshalAs(UnmanagedType.U1)] bool split_on_word; // split on word rather than on token (when used with max_len)
        int max_tokens; // max tokens per segment (0 = no limit)

        // [EXPERIMENTAL] speed-up techniques
        // note: these can significantly reduce the quality of the output
        [MarshalAs(UnmanagedType.U1)] bool speed_up; // speed-up the audio by 2x using Phase Vocoder
        int audio_ctx; // overwrite the audio context size (0 = use default)

        // tokens to provide to the whisper decoder as initial prompt
        // these are prepended to any existing text context from a previous call
        whisper_token_ptr prompt_tokens;
        int prompt_n_tokens;

        // for auto-detection, set to nullptr, "" or "auto"
        public byte* language;

        // common decoding parameters:
        [MarshalAs(UnmanagedType.U1)]
        bool
            suppress_blank; // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/decoding.py#L89

        [MarshalAs(UnmanagedType.U1)]
        bool
            suppress_non_speech_tokens; // ref: https://github.com/openai/whisper/blob/7858aa9c08d98f75575035ecd6481f462d66ca27/whisper/tokenizer.py#L224-L253

        float temperature; // initial decoding temperature, ref: https://ai.stackexchange.com/a/32478

        float
            max_initial_ts; // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/decoding.py#L97

        float
            length_penalty; // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/transcribe.py#L267

        // fallback parameters
        // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/transcribe.py#L274-L278
        float temperature_inc;
        float entropy_thold; // similar to OpenAI's "compression_ratio_threshold"
        float logprob_thold;
        float no_speech_thold; // TODO: not implemented

        [StructLayout(LayoutKind.Sequential)]
        struct greedy_struct
        {
            int
                best_of; // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/transcribe.py#L264
        }

        greedy_struct greedy;

        [StructLayout(LayoutKind.Sequential)]
        struct beam_search_struct
        {
            int
                beam_size; // ref: https://github.com/openai/whisper/blob/f82bc59f5ea234d4b97fb2860842ed38519f7e65/whisper/transcribe.py#L265

            float patience; // TODO: not implemented, ref: https://arxiv.org/pdf/2204.05424.pdf
        }

        beam_search_struct beam_search;

        // called for every newly generated text segment
        void* new_segment_callback;
        void* new_segment_callback_user_data;

        // called each time before the encoder starts
        void* encoder_begin_callback;
        void* encoder_begin_callback_user_data;

        // called by each decoder to filter obtained logits
        void* logits_filter_callback;
        void* logits_filter_callback_user_data;
    }
}
