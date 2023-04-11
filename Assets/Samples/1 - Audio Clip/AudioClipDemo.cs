using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Whisper.Samples
{
    public class AudioClipDemo : MonoBehaviour
    {
        public WhisperManager manager;
        public AudioClip clip;
        public bool echoSound = true;
        public bool streamSegments = true;

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

            timeText.text = $"Time: {sw.ElapsedMilliseconds} ms";
            
            var text = res.Result;
            print(text);
            outputText.text = text;
        }
        
        private void OnNewSegmentHandler(int index, string text)
        {
            _buffer += text;
            outputText.text = _buffer + "...";
        }
    }
}


