using System;
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
            button.onClick.AddListener(OnButtonPressed);
        }

        private async void OnButtonPressed()
        {
            var res = await whisper.GetTextAsync(clip);

            var go = new GameObject();
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.Play();

            while (source.time < clip.length)
            {
                var text = GetSubtitles(res, source.time);
                outputText.text = text;
                await Task.Yield();
            }
        }

        // TODO: this isn't optimized and for demo use only
        private string GetSubtitles(WhisperResult res, float timeSec)
        {
            var sb = new StringBuilder();
            var time = TimeSpan.FromSeconds(timeSec);
            foreach (var seg in res.Segments)
            {
                if (time > seg.End)
                {
                    sb.Append(seg.Text);
                    continue;
                }

                foreach (var token in seg.Tokens)
                {
                    if (time > token.Start)
                        sb.Append(token.Text);

                }
            }

            return sb.ToString();
        }
    }
}