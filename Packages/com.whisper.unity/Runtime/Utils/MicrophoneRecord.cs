using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Whisper.Utils
{
    public class MicrophoneRecord : MonoBehaviour
    {
        public int maxLengthSec = 30;
        public int frequency = 16000;
        public float freq_thold  = 100.0f;
        public bool echo = true;
        public int vadLengthSec = 4;
        
        [Header("Microphone selection (optional)")] 
        [CanBeNull] public Dropdown microphoneDropdown;
        public string microphoneDefaultLabel = "Default microphone";
        
        private float _recordStart;
        private AudioClip _clip;
        private float _length;
        private bool voiceDetected = false;

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
        public bool streaming;
        public bool IsRecording { get; private set; }

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
            _clip = Microphone.Start(RecordStartMicDevice, true, vadLengthSec, frequency);
            InvokeRepeating("VAD", 0.5f, 0.5f);
        }

        private void Update()
        {
            var timePassed = Time.realtimeSinceStartup - _recordStart;
            if (!IsRecording && streaming && voiceDetected){
                print("record");
                Microphone.End(RecordStartMicDevice);
                StartRecord();
            }
            else if (IsRecording && streaming && !voiceDetected && timePassed>2){
                print("recordingStopped");
                StopRecord();
                _clip = Microphone.Start(RecordStartMicDevice, true, vadLengthSec, frequency);
            }
            if (timePassed > maxLengthSec){
                StopRecord();
                _clip = Microphone.Start(RecordStartMicDevice, true, vadLengthSec, frequency);
            }
        }

        private void VAD(){
            float[] samples = new float[_clip.samples * _clip.channels];
            _clip.GetData(samples, 0);
            
            HighPassFilter(samples, freq_thold, frequency);
            float energy_all = 0.0f;
            float energy_last = 0.0f;
            int microphonePos = Microphone.GetPosition(null);
            int vadSamplesLen = frequency * vadLengthSec;

            //get energy all
            int j = microphonePos;
            for (int i=0; i < vadSamplesLen; i++){
                energy_all += samples[j] * samples[j];
                j--;
                if (j < 0){ j = vadSamplesLen-1; }
            }
            // get recent energy
            j = microphonePos;
            for (int i=0; i < vadSamplesLen/2; i++){
                energy_last += samples[j] * samples[j];
                j--;
                if (j < 0){ j = vadSamplesLen-1; }
            }
            energy_all /= vadSamplesLen;
            energy_last /= vadSamplesLen/2;

            if (energy_last > 1.6*energy_all && !voiceDetected && energy_last>0.00000005){
                print("last\nlast: " + energy_last + "\nall: " + energy_all + "\n" + voiceDetected);
                voiceDetected = true;
                return;
            }
            else if (voiceDetected && energy_last*2 < energy_all){
                print("all\nlast: " + energy_last + "\nall: " + energy_all + "\n" + voiceDetected);
                voiceDetected = false;
            }
            if(energy_last==0){_clip = Microphone.Start(RecordStartMicDevice, true, vadLengthSec, frequency);}
            return;
        }

        void HighPassFilter(float[] data, float cutoff, float sampleRate) 
        {
            float Rc = 1.0f / (2.0f * (float)Math.PI * cutoff);
            float Dt = 1.0f / sampleRate;
            float Alpha = Dt / (Rc + Dt);
            float y = data[0];        
            for (int i = 1; i < data.Length; i++)
            {
                y = Alpha * (y + data[i] - data[i - 1]);
                data[i] = y;
            }
        }

        private void OnMicrophoneChanged(int ind)
        {
            if (microphoneDropdown == null) return;
            var opt = microphoneDropdown.options[ind];
            SelectedMicDevice = opt.text == microphoneDefaultLabel ? null : opt.text;
        }

        public void StartRecord(AudioClip startClip=null)
        {
            if (IsRecording)
                return;

            _recordStart = Time.realtimeSinceStartup;
            RecordStartMicDevice = SelectedMicDevice;
            if (startClip == null){
                _clip = Microphone.Start(RecordStartMicDevice, false, maxLengthSec, frequency);
            }
            else{
                _clip = AudioClip.Create("_clip", startClip.samples + Microphone.Start(RecordStartMicDevice, false, maxLengthSec, frequency).samples, startClip.channels, startClip.frequency, false);
            }
            IsRecording = true;
        }

        public void StopRecord()
        {
            if (!IsRecording)
                return;

            var data = GetTrimmedData();
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

            OnRecordStop?.Invoke(data, _clip.frequency, _clip.channels, _length);
        }
        
        public delegate void OnRecordStopDelegate(float[] data, int frequency, int channels, float length);
        public event OnRecordStopDelegate OnRecordStop;

        private float[] GetTrimmedData()
        {
            // get microphone samples and current position
            var pos = Microphone.GetPosition(RecordStartMicDevice);
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
    }
}