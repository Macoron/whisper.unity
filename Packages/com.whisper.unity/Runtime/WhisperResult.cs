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

    public class WhisperSegment
    {
        public readonly int Index;
        public readonly string Text;
        public readonly ulong Start;
        public readonly ulong Stop;

        public WhisperSegment(int index, string text, ulong start, ulong stop)
        {
            Index = index;
            Text = text;
            Start = start;
            Stop = stop;
        }
    }
}