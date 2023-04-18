using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Whisper.Samples
{
    public class MicrophoneDemo : MonoBehaviour
    {
        public WhisperManager whisper;
        public bool streamSegments = true;
        public bool printLanguage = true;

        [Header("Mic settings")] 
        public int maxLengthSec = 30;
        public int frequency = 16000;
        public bool echo = true;

        [Header("UI")] 
        public Button button;
        public Text buttonText;
        public Text outputText;
        public Text timeText;
        private const string MicrophoneDefaultLabel = "Default mic";
        public Dropdown microphoneDropdown;
        public Dropdown languageDropdown;
        public Toggle translateToggle;

        private float _recordStart;
        private bool _isRecording;
        private AudioClip _clip;
        private string _buffer;
        private float _length;
        private string _selectedMicDevice;
        private string _recordStartMicDevice;

        private void Awake()
        {
            button.onClick.AddListener(OnButtonPressed);

            microphoneDropdown.options = new List<Dropdown.OptionData> { new(MicrophoneDefaultLabel) };
            microphoneDropdown.options.AddRange(Microphone.devices.Select(micName => new Dropdown.OptionData(micName)));
            microphoneDropdown.value = microphoneDropdown.options.FindIndex(op => op.text == MicrophoneDefaultLabel);
            microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
            
            languageDropdown.value = languageDropdown.options
                .FindIndex((op) => op.text == whisper.language);
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

            translateToggle.isOn = whisper.translateToEnglish;
            translateToggle.onValueChanged.AddListener(OnTranslateChanged);
            
            if (streamSegments)
                whisper.OnNewSegment += WhisperOnOnNewSegment;
        }
        
        private void Update()
        {
            if (!_isRecording)
                return;

            var timePassed = Time.realtimeSinceStartup - _recordStart;
            if (timePassed > maxLengthSec)
                StopRecord();
        }

        public void OnButtonPressed()
        {
            if (!_isRecording)
                StartRecord();
            else
                StopRecord();

            if (buttonText)
                buttonText.text = _isRecording ? "Stop" : "Record";
        }

        private void OnMicrophoneChanged(int ind)
        {
            var opt = microphoneDropdown.options[ind];
            _selectedMicDevice = opt.text == MicrophoneDefaultLabel ? null : opt.text;
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

        public void StartRecord()
        {
            if (_isRecording)
                return;

            _recordStart = Time.realtimeSinceStartup;
            _recordStartMicDevice = _selectedMicDevice;
            _clip = Microphone.Start(_recordStartMicDevice, false, maxLengthSec, frequency);
            _isRecording = true;
        }

        public void StopRecord()
        {
            if (!_isRecording)
                return;

            var data = GetTrimmedData();
            if (echo)
            {
                var echoClip = AudioClip.Create("echo", data.Length,
                    _clip.channels, _clip.frequency, false);
                echoClip.SetData(data, 0);
                AudioSource.PlayClipAtPoint(echoClip, Vector3.zero);
            }

            Microphone.End(_recordStartMicDevice);
            _isRecording = false;
            _length = Time.realtimeSinceStartup - _recordStart;

            Transcribe(data);
        }

        private float[] GetTrimmedData()
        {
            // get microphone samples and current position
            var pos = Microphone.GetPosition(_recordStartMicDevice);
            var origData = new float[_clip.samples * _clip.channels];
            _clip.GetData(origData, 0);

            // check if mic just reached audio buffer end
            if (pos == 0)
                return origData;

            // looks like we need to trim it by pos
            var trimData = new float[pos];
            Array.Copy(origData, trimData, pos);
            return trimData;
        }

        private async void Transcribe(float[] data)
        {
            _buffer = "";
            
            var sw = new Stopwatch();
            sw.Start();
            
            var res = await whisper.GetTextAsync(data, _clip.frequency, _clip.channels);

            var time = sw.ElapsedMilliseconds;
            var rate =_length / (time * 0.001f);
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