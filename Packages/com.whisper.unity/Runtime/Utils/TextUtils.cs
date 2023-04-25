using System;

namespace Whisper.Utils
{
    public static class TextUtils
    {
        /// <summary>
        /// Write segment start and end timestamps as a human-readable string.
        /// </summary>
        public static string TimestampToString(TimeSpan start, TimeSpan end)
        {
            var startStr = start.ToString(@"mm\:ss\:fff");
            var stopStr = end.ToString(@"mm\:ss\:fff");
            var timestamp = $"[{startStr}->{stopStr}]";
            return timestamp;
        }

        public static string TokenToRichText(WhisperTokenData token, bool printTimestamp)
        {
            var text = token.Text;
            var textColor = ProbabilityToColor(token.Prob);
            var richText = $"<color={textColor}>{text}</color>";
            if (!printTimestamp)
                return richText;
            
            var timestamp = TimestampToString(token.Start, token.End);
            var timestampColor = ProbabilityToColor(token.ProbTimestamp);
            var richTimestamp = $"<color={timestampColor}>{timestamp}</color>";
            
            var comb = richTimestamp + richText;
            return comb;
        }

        private static string ProbabilityToColor(float p)
        {
            if (p <= 0.33f)
                return "red";
            else if (p <= 0.66f)
                return "yellow";
            else
                return "green";
        }
    }
}