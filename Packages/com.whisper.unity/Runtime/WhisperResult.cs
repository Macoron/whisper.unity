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
        /// Individual segments tokens with their meta information.
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

    public class WhisperTokenData
    {
        public readonly int Id;
        public readonly int TimestampId;

        public readonly float Prob;
        public readonly float ProbLog;
        public readonly float ProbTimestamp; 
        public readonly float ProbTimestampSum;

        public readonly TimeSpan Start;
        public readonly TimeSpan End;

        public readonly float VoiceLength;
        
        public readonly string Text;

        public WhisperTokenData(WhisperNativeTokenData nativeToken, string text, bool timestamps)
        {
            Id = nativeToken.id;
            TimestampId = nativeToken.tid;
            Prob = nativeToken.p;
            ProbLog = nativeToken.plog;
            if (timestamps)
            {
                ProbTimestamp = nativeToken.pt;
                ProbTimestampSum = nativeToken.ptsum;
                Start = TimeSpan.FromMilliseconds(nativeToken.t0 * 10);
                End = TimeSpan.FromMilliseconds(nativeToken.t1 * 10);
                VoiceLength = nativeToken.vlen;
            }
            
            Text = text;
        }
        
        
    }
}