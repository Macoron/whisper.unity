using System;
using System.Collections.Generic;
using System.Text;
using Whisper.Native;

namespace Whisper
{
    public class WhisperResult
    {
        public readonly List<WhisperSegment> Segments;
        public readonly string Result;
        public readonly int LanguageId;
        public readonly string Language;

        public WhisperResult(List<WhisperSegment> segments, int languageId)
        {
            Segments = segments;
            LanguageId = languageId;
            Language = WhisperLanguage.GetLanguageString(languageId);
            
            // generate full string based on segments
            var builder = new StringBuilder();
            foreach (var seg in segments)
            {
                builder.Append(seg.Text);
            }
            Result = builder.ToString();
        }
    }

    /// <summary>
    /// Single segment of whisper audio transcription.
    /// Can be a few words, a sentence, or even a paragraph.
    /// </summary>
    public class WhisperSegment
    {
        /// <summary>
        /// Segment index in current Whisper context.
        /// </summary>
        public readonly int Index;
        
        /// <summary>
        /// Combined text of all tokens in this segment.
        /// </summary>
        public readonly string Text;
        
        /// <summary>
        /// Segment start timestamp based on transcribed audio.
        /// </summary>
        public readonly TimeSpan Start;
        
        /// <summary>
        /// Segment end timestamp based on transcribed audio.
        /// </summary>
        public readonly TimeSpan End;

        /// <summary>
        /// Optional individual tokens with their meta information.
        /// Null if params <see cref="WhisperParams.EnableTokens"/> is false.
        /// </summary>
        public WhisperTokenData[] Tokens;

        public WhisperSegment(int index, string text, ulong start, ulong end)
        {
            Index = index;
            Text = text;
            Start = TimeSpan.FromMilliseconds(start * 10);
            End = TimeSpan.FromMilliseconds(end * 10);
        }

        /// <summary>
        /// Write segment start and end timestamps as a human-readable string.
        /// </summary>
        public string TimestampToString()
        {
            var startStr = Start.ToString(@"mm\:ss\:fff");
            var stopStr = End.ToString(@"mm\:ss\:fff");
            var timestamp = $"[{startStr}->{stopStr}]";
            return timestamp;
        }
    }

    /// <summary>
    /// Usually represent a word, part of the word, punctuation marks
    /// or some special tokens like [EOT], [BEG], etc.
    /// </summary>
    public class WhisperTokenData
    {
        /// <summary>
        /// Id of the token in whisper model vocabulary.
        /// </summary>
        public readonly int Id;
        /// <summary>
        /// Probability (confidence) of the token in [0, 1] range.
        /// </summary>
        public readonly float Prob;
        /// <summary>
        /// Log probability (confidence) of the token.
        /// </summary>
        public readonly float ProbLog;
        /// <summary>
        /// Text representation of the token.
        /// </summary>
        public readonly string Text;
        /// <summary>
        /// True if this token is special token used by whisper, like [EOT], [BEG], etc.
        /// </summary>
        public readonly bool IsSpecial;
        /// <summary>
        /// Optional token timestamp information.
        /// Null if params <see cref="WhisperParams.TokenTimestamps"/> is false.
        /// </summary>
        public readonly WhisperTokenTimestamp Timestamp;

        public WhisperTokenData(WhisperNativeTokenData nativeToken, string text, bool timestamps, bool isSpecial)
        {
            Id = nativeToken.id;
            Prob = nativeToken.p;
            ProbLog = nativeToken.plog;
            Text = text;
            IsSpecial = isSpecial;

            if (timestamps)
                Timestamp = new WhisperTokenTimestamp(nativeToken);
        }
    }

    /// <summary>
    /// Optional token timestamp information.
    /// </summary>
    public class WhisperTokenTimestamp
    {
        /// <summary>
        /// Forced timestamp token id.
        /// </summary>
        public readonly int Id;
        /// <summary>
        /// Probability (confidence) of the timestamp in [0, 1] range.
        /// </summary>
        public readonly float Prob; 
        /// <summary>
        /// Sum of probabilities of all timestamp tokens.
        /// </summary>
        public readonly float ProbSum;
        /// <summary>
        /// Timestamp of the token start. Relative to the whole audio.
        /// </summary>
        public readonly TimeSpan Start;
        /// <summary>
        /// Timestamp of the token end. Relative to the whole audio.
        /// </summary>
        public readonly TimeSpan End;
        /// <summary>
        /// Voice length of the token.
        /// </summary>
        public readonly float VoiceLength;

        public WhisperTokenTimestamp(WhisperNativeTokenData nativeToken)
        {
            Id = nativeToken.tid;
            Prob = nativeToken.pt;
            ProbSum = nativeToken.ptsum;
            Start = TimeSpan.FromMilliseconds(nativeToken.t0 * 10);
            End = TimeSpan.FromMilliseconds(nativeToken.t1 * 10);
            VoiceLength = nativeToken.vlen;
        }
    }
}