using System;
using System.Collections.Generic;
using System.Text;

namespace Whisper
{
    public class WhisperResult
    {
        public readonly List<string> Segments;
        public readonly string Result;
        public readonly int LanguageId;
        public readonly string Language;

        public WhisperResult(List<string> segments, int languageId)
        {
            Segments = segments;
            LanguageId = languageId;
            Language = WhisperLanguage.GetLanguageString(languageId);
            
            // generate full string based on segments
            var builder = new StringBuilder();
            foreach (var seg in segments)
            {
                builder.Append(seg);
            }
            Result = builder.ToString();
        }
    }

    /// <summary>
    /// Segment of whisper audio transcription.
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

        public WhisperSegment(int index, string text, ulong start, ulong end)
        {
            Index = index;
            Text = text;
            Start = TimeSpan.FromMilliseconds(start * 10);
            End = TimeSpan.FromMilliseconds(end * 10);
        }
    }
}