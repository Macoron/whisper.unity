using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;

namespace Whisper.Samples
{
    /// <summary>
    /// Takes audio clip and make a transcription.
    /// </summary>
    public class AudioClipDemo : MonoBehaviour
    {
        public WhisperManager manager;
        public AudioClip clip;
        public bool streamSegments = true;
        public bool echoSound = true;
        public bool printLanguage = true;

        [Header("UI")]
        public Button button;
        public Text outputText;
        public Text timeText;
        public ScrollRect scroll;
        public Dropdown languageDropdown;
        public Toggle translateToggle;
        
        private string _buffer;
        
        private void Awake()
        {
            manager.OnNewSegment += OnNewSegment;
            manager.OnProgress += OnProgressHandler;
            
            button.onClick.AddListener(ButtonPressed);
            languageDropdown.value = languageDropdown.options
                .FindIndex(op => op.text == manager.language);
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

            translateToggle.isOn = manager.translateToEnglish;
            translateToggle.onValueChanged.AddListener(OnTranslateChanged);
        }

        public async void ButtonPressed()
        {
            _buffer = "";
            if (echoSound)
                AudioSource.PlayClipAtPoint(clip, Vector3.zero);

            var sw = new Stopwatch();
            sw.Start();
            
            var res = await manager.GetTextAsync(clip);
            if (res == null || !outputText) 
                return;

            var time = sw.ElapsedMilliseconds;
            var rate = clip.length / (time * 0.001f);
            timeText.text = $"Time: {time} ms\nRate: {rate:F1}x";

            var text = res.Result;
            if (printLanguage)
                text += $"\n\nLanguage: {res.Language}";
            
            outputText.text = text;
            UiUtils.ScrollDown(scroll);
        }
        
        private void OnLanguageChanged(int ind)
        {
            var opt = languageDropdown.options[ind];
            manager.language = opt.text;
        }
        
        private void OnTranslateChanged(bool translate)
        {
            manager.translateToEnglish = translate;
        }

        private void OnProgressHandler(int progress)
        {
            if (!timeText)
                return;
            timeText.text = $"Progress: {progress}%";
        }
        
        private void OnNewSegment(WhisperSegment segment)
        {
            if (!streamSegments || !outputText)
                return;

            _buffer += segment.Text;
            outputText.text = _buffer + "...";
            UiUtils.ScrollDown(scroll);
        }
    }
}


