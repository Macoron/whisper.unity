using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Whisper.Samples
{
    public class MicrophoneRecord : MonoBehaviour
    {
        [Header("Mic settings")]
        public int maxLengthSec = 30;
        public int frequency = 16000;
        public bool echo = true;
        
        private float _recordStart;
        private AudioClip _clip;
        private float _length;

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

        public IEnumerable<string> AvailableMicDevices => Microphone.devices;

        private void Update()
        {
            if (!IsRecording)
                return;

            var timePassed = Time.realtimeSinceStartup - _recordStart;
            if (timePassed > maxLengthSec)
                StopRecord();
        }

        public void StartRecord()
        {
            if (IsRecording)
                return;

            _recordStart = Time.realtimeSinceStartup;
            RecordStartMicDevice = SelectedMicDevice;
            _clip = Microphone.Start(RecordStartMicDevice, false, maxLengthSec, frequency);
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

            OnRecordStop?.Invoke(data, _clip, _length);
        }
        
        public delegate void OnRecordStopDelegate(float[] data, AudioClip clip, float length);
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