using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace Whisper.Samples
{
    public class AudioClipDemo : MonoBehaviour
    {
        public WhisperManager manager;
        public AudioClip clip;
        public bool echoSound = true;
        public bool streamSegments = true;
        public bool printLanguage = true;
        public bool showTimestamps = true;

        [Header("UI")]
        public Button button;
        public Text outputText;
        public Text timeText;

        private string _buffer;

        private void Awake()
        {
            button.onClick.AddListener(ButtonPressed);
            if (streamSegments)
                manager.OnNewSegment += OnNewSegmentHandler;
        }

        public async void ButtonPressed()
        {
            _buffer = "";
            if (echoSound)
                AudioSource.PlayClipAtPoint(clip, Vector3.zero);

            var sw = new Stopwatch();
            sw.Start();
            
            var res = await manager.GetTextAsync(clip);
            if (res == null) 
                return;

            var time = sw.ElapsedMilliseconds;
            var rate = clip.length / (time * 0.001f);
            timeText.text = $"Time: {time} ms\nRate: {rate:F1}x";
            
            var text = res.Result;
            print(text);

            if (printLanguage)
                text += $"\n\nLanguage: {res.Language}";
            //outputText.text = text;
        }
        
        private void OnNewSegmentHandler(WhisperSegment segment)
        {
            var timestamp = "";
            if (showTimestamps)
            {
                var startStr = segment.Start.ToString(@"mm\:ss\:fff");
                var stopStr = segment.End.ToString(@"mm\:ss\:fff");
                timestamp = $"<b>[{startStr}->{stopStr}]</b>";
            }
            
            _buffer += timestamp + segment.Text + "\n";
            outputText.text = _buffer + "...";
        }
    }
}


