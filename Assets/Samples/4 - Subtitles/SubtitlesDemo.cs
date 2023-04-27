using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Whisper.Samples
{
    public class SubtitlesDemo : MonoBehaviour
    {
        public WhisperManager whisper;
        public AudioClip clip;
        
        [Header("UI")] 
        public Button button;
        public Text outputText;
        public Text timeText;
        public Dropdown languageDropdown;
        public Toggle translateToggle;

        private void Awake()
        {
            languageDropdown.value = languageDropdown.options
                .FindIndex(op => op.text == whisper.language);
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

            translateToggle.isOn = whisper.translateToEnglish;
            translateToggle.onValueChanged.AddListener(OnTranslateChanged);
            
            button.onClick.AddListener(OnButtonPressed);
        }
        
        private void OnLanguageChanged(int ind)
        {
            var opt = languageDropdown.options[ind];
            whisper.language = opt.text;
        }
        
        private void OnTranslateChanged(bool translate)
        {
            whisper.translateToEnglish = translate;
        }

        private async void OnButtonPressed()
        {
            outputText.text = "Transcribing...";
            var sw = new Stopwatch();
            sw.Start();
            
            var res = await whisper.GetTextAsync(clip);
            
            var time = sw.ElapsedMilliseconds;
            var rate = clip.length / (time * 0.001f);
            timeText.text = $"Time: {time} ms\nRate: {rate:F1}x";

            // start playing sound
            var go = new GameObject("Audio Echo");
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.Play();

            // and show subtitles at the same time
            while (source.time < clip.length)
            {
                var text = GetSubtitles(res, source.time);
                outputText.text = text;
                await Task.Yield();
                
                // check that audio source still here and wasn't dedtroyed
                if (!source)
                    return;
            }

            outputText.text = ResultToRichText(res);
            Destroy(go);
        }

        // TODO: this isn't optimized and for demo use only
        private string GetSubtitles(WhisperResult res, float timeSec)
        {
            var sb = new StringBuilder();
            var time = TimeSpan.FromSeconds(timeSec);
            foreach (var seg in res.Segments)
            {
                // check if we already passed whole segment
                if (time >= seg.End)
                {
                    sb.Append(SegmentToRichText(seg));
                    continue;
                }

                foreach (var token in seg.Tokens)
                {
                    if (time > token.Start)
                    {
                        var text = TokenToRichText(token);
                        sb.Append(text);
                    }
                }
            }

            return sb.ToString();
        }

        private static string ResultToRichText(WhisperResult result)
        {
            var sb = new StringBuilder();
            foreach (var seg in result.Segments)
            {
                var str = SegmentToRichText(seg);
                sb.Append(str);
            }

            return sb.ToString();
        }

        private static string SegmentToRichText(WhisperSegment segment)
        {
            var sb = new StringBuilder();
            foreach (var token in segment.Tokens)
            {
                var tokenText = TokenToRichText(token);
                sb.Append(tokenText);
            }

            return sb.ToString();
        }
        
        private static string TokenToRichText(WhisperTokenData token)
        {
            if (token.IsSpecial)
                return "";
            
            var text = token.Text;
            var textColor = ProbabilityToColor(token.Prob);
            var richText = $"<color={textColor}>{text}</color>";
            return richText;
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