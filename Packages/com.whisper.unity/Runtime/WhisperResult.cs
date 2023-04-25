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

        public readonly List<WhisperTokenData> Tokens = new List<WhisperTokenData>();

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

    public class WhisperTokenData
    {
        public readonly int Id;
        public readonly string Text;
        public readonly float Probability;
        public readonly float ProbabilityLog;

        public WhisperTokenData(WhisperNativeTokenData nativeToken, string text)
        {
            Id = nativeToken.id;
            Probability = nativeToken.p;
            ProbabilityLog = nativeToken.plog;
            Text = text;
        }
    }
}