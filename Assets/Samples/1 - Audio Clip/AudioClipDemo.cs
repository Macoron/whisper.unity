using System;
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

        [Header("UI")]
        public Button button;
        public Text outputText;
        public Text timeText;

        private void Awake()
        {
            button.onClick.AddListener(ButtonPressed);
        }

        public async void ButtonPressed()
        {
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
    }
}


