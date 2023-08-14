using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Whisper.Utils
{
    public struct AudioChunk
    {
        public float[] Data;
        public int Frequency;
        public int Channels;
        public float Length;
        public bool IsVoiceDetected;
    }
    
    public delegate void OnVadChangedDelegate(bool isSpeechDetected);
    public delegate void OnChunkReadyDelegate(AudioChunk chunk);
    public delegate void OnRecordStopDelegate(float[] data, int frequency, int channels, float length);
    
    public class MicrophoneRecord : MonoBehaviour
    {
        public int maxLengthSec = 30;
        public int frequency = 16000;
        public float chunksLengthSec = 0.5f;
        public bool echo = true;
        
        [Header("Voice Activity Detection (VAD)")]
        public bool useVad = true;
        public float vadUpdateRateSec = 0.1f;
        public float vadContextSec = 30f;
        public float vadLastSec = 1.25f;
        public float vadThd = 0.6f;
        public float vadFreqThd = 100.0f;
        [CanBeNull] public Image vadIndicatorImage;
        
        [Header("VAD Stop")] 
        public bool vadStop;
        public bool dropVadPart = true;
        public float vadStopTime = 3f;

        [Header("Microphone selection (optional)")] 
        [CanBeNull] public Dropdown microphoneDropdown;
        public string microphoneDefaultLabel = "Default microphone";

        public event OnVadChangedDelegate OnVadChanged;
        public event OnChunkReadyDelegate OnChunkReady;
        public event OnRecordStopDelegate OnRecordStop;

        private float _recordStart;
        private int _lastVadPos;
        private AudioClip _clip;
        private float _length;
        private int _lastChunkPos;
        private int _chunksLength;
        private float? _vadStopBegin;
        
        private string _selectedMicDevice;
        public string SelectedMicDevice
        {
            get => _selectedMicDevice;
            set
            {
                if (value != null && !AvailableMicDevices.Contains(value))
                    throw new ArgumentException("Microphone device not found");
                _selectedMicDevice = value;
            }
        }

        public string RecordStartMicDevice { get; private set; }
        public bool IsRecording { get; private set; }
        public bool IsVoiceDetected { get; private set; }

        public IEnumerable<string> AvailableMicDevices => Microphone.devices;

        private void Awake()
        {
            if(microphoneDropdown != null)
            {
                microphoneDropdown.options = AvailableMicDevices
                    .Prepend(microphoneDefaultLabel)
                    .Select(text => new Dropdown.OptionData(text))
                    .ToList();
                microphoneDropdown.value = microphoneDropdown.options
                    .FindIndex(op => op.text == microphoneDefaultLabel);
                microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
            }
        }

        private void Update()
        {
            if (!IsRecording)
                return;
            
            // check that recording reached max time
            var timePassed = Time.realtimeSinceStartup - _recordStart;
            if (timePassed > maxLengthSec)
            {
                StopRecord();
                return;
            }
            
            // still recording - update chunks and vad
            UpdateChunks();
            UpdateVad();
        }
        
        private void UpdateChunks()
        {
            // is anyone even subscribe to do this?
            if (OnChunkReady == null)
                return;

            // check if chunks length is valid
            if (_chunksLength <= 0)
                return;
            
            // get current chunk length
            var samplesCount = Microphone.GetPosition(RecordStartMicDevice);
            var chunk = samplesCount - _lastChunkPos;
            
            // send new chunks while there has valid size
            while (chunk > _chunksLength)
            {
                var origData = new float[_chunksLength];
                _clip.GetData(origData, _lastChunkPos);

                var chunkStruct = new AudioChunk()
                {
                    Data = origData,
                    Frequency = _clip.frequency,
                    Channels = _clip.channels,
                    Length = chunksLengthSec,
                    IsVoiceDetected = IsVoiceDetected
                };
                OnChunkReady(chunkStruct);

                _lastChunkPos += _chunksLength;
                chunk = samplesCount - _lastChunkPos;
            }
        }
        
        private void UpdateVad()
        {
            if (!useVad)
                return;
            
            // get current position of microphone header
            var samplesCount = Microphone.GetPosition(RecordStartMicDevice);
            if (samplesCount <= 0)
                return;

            // check if it's time to update
            var vadUpdateRateSamples = vadUpdateRateSec * _clip.frequency;
            var dt = samplesCount - _lastVadPos;
            if (dt < vadUpdateRateSamples)
                return;
            _lastVadPos = samplesCount;
            
            // try to get sample for voice detection
            var vadContextSamples = (int) (_clip.frequency * vadContextSec);
            var dataLength = Math.Min(vadContextSamples, samplesCount); 
            var offset = Math.Max(samplesCount - vadContextSamples, 0);

            var data = new float[dataLength];
            _clip.GetData(data, offset);

            var vad = AudioUtils.SimpleVad(data, _clip.frequency, vadLastSec, vadThd, vadFreqThd);
            if (vadIndicatorImage)
            {
                var color = vad ? Color.green : Color.red;
                vadIndicatorImage.color = color;
            }

            if (vad != IsVoiceDetected)
            {
                _vadStopBegin = !vad ? Time.realtimeSinceStartup : (float?) null;
                IsVoiceDetected = vad;
                OnVadChanged?.Invoke(vad);   
            }

            UpdateVadStop();
        }
        
        private void UpdateVadStop()
        {
            if (!vadStop || _vadStopBegin == null)
                return;

            var passedTime = Time.realtimeSinceStartup - _vadStopBegin;
            if (passedTime > vadStopTime)
            {
                var dropTime = dropVadPart ? vadStopTime : 0f;
                StopRecord(dropTime);
            }
        }

        private void OnMicrophoneChanged(int ind)
        {
            if (microphoneDropdown == null) return;
            var opt = microphoneDropdown.options[ind];
            SelectedMicDevice = opt.text == microphoneDefaultLabel ? null : opt.text;
        }

        public void StartRecord()
        {
            if (IsRecording)
                return;

            _recordStart = Time.realtimeSinceStartup;
            RecordStartMicDevice = SelectedMicDevice;
            _clip = Microphone.Start(RecordStartMicDevice, false, maxLengthSec, frequency);
            IsRecording = true;
            
            _lastChunkPos = 0;
            _lastVadPos = 0;
            _vadStopBegin = null;
            _chunksLength = (int) (_clip.frequency * _clip.channels * chunksLengthSec);
        }

        public void StopRecord(float dropTimeSec = 0f)
        {
            if (!IsRecording)
                return;

            var data = GetTrimmedData(dropTimeSec);
            if (echo)
            {
                var echoClip = AudioClip.Create("echo", data.Length,
                    _clip.channels, _clip.frequency, false);
                echoClip.SetData(data, 0);
                AudioSource.PlayClipAtPoint(echoClip, Vector3.zero);
            }

            Microphone.End(RecordStartMicDevice);
            IsRecording = false;
            _length = Time.realtimeSinceStartup - _recordStart;
            
            if (IsVoiceDetected)
            {
                IsVoiceDetected = false;
                OnVadChanged?.Invoke(false);   
            }

            OnRecordStop?.Invoke(data, _clip.frequency, _clip.channels, _length);
        }

        private float[] GetTrimmedData(float dropTimeSec = 0f)
        {
            // get microphone samples and current position
            var pos = Microphone.GetPosition(RecordStartMicDevice);
            var len = pos == 0 ? _clip.samples * _clip.channels : pos;
            var dropTimeSamples = (int) (_clip.frequency * _clip.channels * dropTimeSec);
            len = Math.Max(0, len - dropTimeSamples);
            
            var data = new float[len];
            _clip.GetData(data, 0);
            return data;
        }
    }
}