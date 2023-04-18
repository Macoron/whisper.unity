using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Whisper.Samples
{
    public class MicrophoneDemo : MonoBehaviour
    {
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;
        public bool streamSegments = true;
        public bool printLanguage = true;

        [Header("UI")] 
        public Button button;
        public Text buttonText;
        public Text outputText;
        public Text timeText;
        public Dropdown microphoneDropdown;
        public Dropdown languageDropdown;
        public Toggle translateToggle;

        private const string MicrophoneDefaultLabel = "Default mic";
        
        private string _buffer;

        private void Awake()
        {
            button.onClick.AddListener(OnButtonPressed);

            microphoneDropdown.options = microphoneRecord.AvailableMicDevices
                .Prepend(MicrophoneDefaultLabel)
                .Select(text => new Dropdown.OptionData(text))
                .ToList();
            microphoneDropdown.value = microphoneDropdown.options
                .FindIndex(op => op.text == MicrophoneDefaultLabel);
            microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
            
            languageDropdown.value = languageDropdown.options
                .FindIndex(op => op.text == whisper.language);
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

            translateToggle.isOn = whisper.translateToEnglish;
            translateToggle.onValueChanged.AddListener(OnTranslateChanged);

            microphoneRecord.OnRecordStop += Transcribe;
            
            if (streamSegments)
                whisper.OnNewSegment += WhisperOnOnNewSegment;
        }

        private void OnButtonPressed()
        {
            if (!microphoneRecord.IsRecording)
                microphoneRecord.StartRecord();
            else
                microphoneRecord.StopRecord();

            if (buttonText)
                buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";
        }

        private void OnMicrophoneChanged(int ind)
        {
            var opt = microphoneDropdown.options[ind];
            microphoneRecord.SelectedMicDevice = opt.text == MicrophoneDefaultLabel ? null : opt.text;
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

        private async void Transcribe(float[] data, AudioClip clip, float length)
        {
            _buffer = "";
            
            var sw = new Stopwatch();
            sw.Start();
            
            var res = await whisper.GetTextAsync(data, clip.frequency, clip.channels);

            var time = sw.ElapsedMilliseconds;
            var rate = length / (time * 0.001f);
            timeText.text = $"Time: {time} ms\nRate: {rate:F1}x";
            if (res == null)
                return;

            var text = res.Result;
            if (printLanguage)
                text += $"\n\nLanguage: {res.Language}";
            outputText.text = text;
        }
        
        private void WhisperOnOnNewSegment(int index, string text)
        {
            _buffer += text;
            outputText.text = _buffer + "...";
        }
    }
}