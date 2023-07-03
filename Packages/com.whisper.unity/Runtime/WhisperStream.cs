using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Whisper.Utils;

namespace Whisper
{
    public delegate void OnStreamResultUpdatedDelegate(string updatedResult);

    /// <summary>
    /// Algorithm used for audio stream transcription.
    /// </summary>
    public enum WhisperStreamStrategy
    {
        /// <summary>
         /// Transcribe audio by dividing it into chunks.
        /// Previous transcription added as a prompt for next chunks.
         /// </summary>
         SlidingWindow,
         
         /// <summary>
         /// Transcribe audio dividing it on chunks by voice activation detection (VAD).
         /// </summary>
         SimpleVad
    }
    
    public class WhisperStreamParams
    {
        /// <summary>
        /// Algorithm used for audio stream transcription.
        /// </summary>
        public readonly WhisperStreamStrategy Strategy;

        /// <summary>
        /// Regular whisper inference params.
        /// </summary>
        public readonly WhisperParams InferenceParam;
        
        /// <summary>
        /// Audio stream frequency. Can't change during transcription.
        /// </summary>
        public readonly int Frequency;
        
        /// <summary>
        /// Audio stream channels count. Can't change during transcription.
        /// </summary>
        public readonly int Channels;
        
        /// <summary>
        /// Minimal portions of audio that will be processed by whisper stream in seconds.
        /// </summary>
        public readonly float StepSec;
        
        /// <summary>
        /// Minimal portions of audio that will be processed by whisper in audio samples.    
        /// </summary>
        public readonly int StepSamples;

        public readonly float KeepSec;

        public readonly int KeepSamples;

        public readonly float LengthSec;
        
        public readonly int LengthSamples;

        public readonly bool UpdatePrompt;

        public readonly int StepsCount;

        public readonly bool DropOldBuffer;

        public WhisperStreamParams(WhisperStreamStrategy strategy, 
            WhisperParams inferenceParam, int frequency, int channels,
            float stepSec = 3f, float keepSec = 0.2f, float lengthSec = 10f,
            bool updatePrompt = true, bool dropOldBuffer = false)
        {
            Strategy = strategy;
            InferenceParam = inferenceParam;
            Frequency = frequency;
            Channels = channels;
            
            StepSec = stepSec;
            StepSamples = (int) (stepSec * frequency * channels);

            KeepSec = keepSec;
            KeepSamples = (int) (keepSec * frequency * channels);

            LengthSec = lengthSec;
            LengthSamples = (int) (lengthSec * frequency * channels);

            StepsCount = Math.Max(1, (int) (LengthSec / StepSec) - 1);
            
            UpdatePrompt = updatePrompt;
            DropOldBuffer = dropOldBuffer;
        }
    }
    
    /// <summary>
    /// Handling all streaming logic (sliding-window, VAD, etc).
    /// </summary>
    public class WhisperStream
    {
        public event OnStreamResultUpdatedDelegate OnResultUpdated;
        
        private readonly WhisperWrapper _wrapper;
        private readonly WhisperStreamParams _param;
        private readonly string _originalPrompt;
        private readonly MicrophoneRecord _microphone;
        
        private readonly List<float> _newBuffer = new List<float>();
        private readonly List<float> _oldBuffer = new List<float>();
        private int _step;
        
        private Task<WhisperResult> _task;
        private string _output = "";

        public WhisperStream(WhisperWrapper wrapper, WhisperStreamParams param,
            MicrophoneRecord microphone = null)
        {
            _wrapper = wrapper;
            _param = param;
            _originalPrompt = _param.InferenceParam.InitialPrompt;

            // if we set microphone - streaming works in auto mode
            if (microphone != null)
            {
                _microphone = microphone;
                _microphone.OnChunkReady += MicrophoneOnChunkReady;
                _microphone.OnRecordStop += MicrophoneOnRecordStop;
            }
        }
        
        public void AddToStream(float[] samples)
        {
            // add new samples to buffer
            _newBuffer.AddRange(samples);
            
            // do actual strategy
            UpdateSlidingWindow();
        }

        public async void StopStream()
        {
            // first wait until last task complete
            await _task;
            
            // do actual strategy
            UpdateSlidingWindow(true);
        }
        
        private async void UpdateSlidingWindow(bool lastCall = false)
        {
            // check if task isn't busy
            // if it's still transcribing - just skip it
            // next iteration will handle current and future data
            if (_task != null && !_task.IsCompleted)
                return;
            
            // check if we have enough data to start transcribing
            // if it's last call - just grab all whats left
            var newBufferLen = _newBuffer.Count;
            if (!lastCall && newBufferLen < _param.StepSamples)
                return;

            // calculate how much we can get from _oldBuffer
            var oldBufferLen = _oldBuffer.Count;
            int nSamplesTake;
            if (_param.DropOldBuffer)
            {
                // original ggml implementation
                // take up to _param.LengthSamples audio from previous iteration
                nSamplesTake = Math.Min(oldBufferLen,
                    Math.Max(0, _param.KeepSamples + _param.LengthSamples - newBufferLen));
            }
            else
            {
                // just take everything from _oldBuffer
                nSamplesTake = oldBufferLen;
            }

            // copy data from old buffer to temp inference one
            var bufferLen = nSamplesTake + newBufferLen;
            var buffer = new float[bufferLen];
            var oldBufferStart = oldBufferLen - nSamplesTake;
            _oldBuffer.CopyTo(oldBufferStart, buffer, 0, nSamplesTake);
            
            // and now add data from new buffer
            _newBuffer.CopyTo(0, buffer, nSamplesTake, newBufferLen);
            
            // save buffer for next iterations
            _oldBuffer.Clear();
            _oldBuffer.AddRange(buffer);
            
            // before we start - clear buffer for next audio data
            // current data is already copied into local buffer
            _newBuffer.Clear();

            // start transcribing sliding window content
            _task = _wrapper.GetTextAsync(buffer, _param.Frequency, 
                _param.Channels, _param.InferenceParam);
            
            // append current transcription into temporary output
            var res = await _task;
            var currentSegment = res.Result;
            var currentOutput = _output + currentSegment;
            Debug.Log($"-STREAMING- {currentOutput}");
            
            // send update to user
            OnResultUpdated?.Invoke(currentOutput);

            // check if finished working on current "line"
            _step++;
            if (_step % _param.StepsCount == 0)
            {
                Debug.Log($"-STREAMING- CLICK");
                
                _output = currentOutput;

                // update prompt with latest transcription
                if (_param.UpdatePrompt)
                    _param.InferenceParam.InitialPrompt = _originalPrompt + _output;

                // get
                var updBufferLen = _param.KeepSamples;
                if (updBufferLen > bufferLen)
                    updBufferLen = bufferLen;
                
                _oldBuffer.Clear();
                _oldBuffer.AddRange(new ArraySegment<float>(buffer, bufferLen - updBufferLen, updBufferLen));
            }

            // reset if its last call
            if (lastCall)
                Reset();
        }

        private void Reset()
        {
            _output = "";
            _step = 0;
            _oldBuffer.Clear();
            _newBuffer.Clear();
        }
        
        private void MicrophoneOnChunkReady(AudioChunk chunk)
        {
            AddToStream(chunk.Data);
        }
        
        private void MicrophoneOnRecordStop(float[] data, int frequency, int channels, float length)
        {
            StopStream();
        }

    }
}