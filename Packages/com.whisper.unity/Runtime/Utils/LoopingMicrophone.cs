using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Whisper.Utils
{
    public class LoopingMicrophone : MonoBehaviour
    {
        public int maxLengthSec = 10;
        public int frequency = 16000;
        public float evaluationTime = 0.5f;

        private float elapsedTime = 0f;

        [Header("Microphone selection (optional)")]
        [CanBeNull] public Dropdown microphoneDropdown;
        public string microphoneDefaultLabel = "Default microphone";

        private AudioClip _clip;

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

        public delegate void OnEvaluateDelegate(AudioClip clip);
        public delegate void OnRecordStopDelegate();
        public event OnEvaluateDelegate OnEvaluate;
        public event OnRecordStopDelegate OnRecordStop;

        private void Awake()
        {
            IsRecording = false;
            if (microphoneDropdown != null)
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

            elapsedTime += Time.deltaTime;
            if (elapsedTime >= evaluationTime)
                elapsedTime -= evaluationTime;
                OnEvaluate?.Invoke(_clip);
        }

        private void OnMicrophoneChanged(int ind)
        {
            if (microphoneDropdown == null) return;
            var opt = microphoneDropdown.options[ind];
            SelectedMicDevice = opt.text == microphoneDefaultLabel ? null : opt.text;
        }

        public void StartRecord()
        {
            RecordStartMicDevice = SelectedMicDevice;
            _clip = Microphone.Start(RecordStartMicDevice, true, maxLengthSec, frequency);
            IsRecording = true;
        }

        public void StopRecord()
        {
            if (!IsRecording)
                return;

            Microphone.End(RecordStartMicDevice);
            IsRecording = false;

            OnRecordStop?.Invoke();
        }

        public float[] GetData()
        {
            int pos = Microphone.GetPosition(RecordStartMicDevice);
            if(pos == 0)
            {
                float[] arr = new float[_clip.samples];
                _clip.GetData(arr, 0);
                return arr;
            }
            float[] arr1 = new float[_clip.samples - pos];
            float[] arr2 = new float[pos];
            _clip.GetData(arr1, pos);
            _clip.GetData(arr2, 0);
            return arr1.Concat(arr2).ToArray();
        }

    }
}