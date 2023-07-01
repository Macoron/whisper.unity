using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
         /// Repeatedly transcribe audio from beginning to end by adding new data.
         /// Usable only for short audio and very fast inference.
         /// In the end will result best transcription.
         /// </summary>
         Recurrent,
         
         /// <summary>
         /// Transcribe audio by dividing it on chunks.
         /// Minimal chunk length is StepSec + KeepSec.
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

        public WhisperStreamParams(WhisperStreamStrategy strategy, 
            WhisperParams inferenceParam, int frequency, int channels,
            float stepSec = 3f, float keepSec = 0.2f, float lengthSec = 10f,
            bool updatePrompt = true)
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

            StepsCount = Math.Max(1, LengthSamples / StepSamples);
            
            UpdatePrompt = updatePrompt;
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
        
        private int _pointer;
        private readonly List<float> _buffer = new List<float>();
        private Task<WhisperResult> _task;
        private string _output = "";
        private string _currentSegment = "";
        private int _step;

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
            _buffer.AddRange(samples);
            
            // check if task isn't busy
            if (_task != null && !_task.IsCompleted)
                return;

            // do actual strategy
            switch (_param.Strategy)
            {
                case WhisperStreamStrategy.Recurrent:
                    UpdateRecurrent();
                    break;
                case WhisperStreamStrategy.SlidingWindow:
                    UpdateSlidingWindow();
                    break;
            }
        }

        public async void StopStream()
        {
            // first wait until last task complete
            await _task;
            
            // do actual strategy
            switch (_param.Strategy)
            {
                case WhisperStreamStrategy.Recurrent:
                    UpdateRecurrent(true);
                    break;
                case WhisperStreamStrategy.SlidingWindow:
                    UpdateSlidingWindow(true);
                    break;
            }
        }
        
        private async void UpdateRecurrent(bool untilEnd = false)
        {
            // check if we have enough data to start transcribing
            var size = _buffer.Count - _pointer;
            if (size < _param.StepSamples)
                return;
            _pointer = _buffer.Count - 1;
            
            // send a buffer for processing
            var bufferArr = _buffer.ToArray();
            _task = _wrapper.GetTextAsync(bufferArr, _param.Frequency, _param.Channels, _param.InferenceParam);
            var res = await _task;
            
            // send update to user
            OnResultUpdated?.Invoke(res.Result);
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
            var size = _buffer.Count - _pointer;
            if (!lastCall && size < _param.StepSamples)
                return;

            // get length of prev buffer
            var length = _buffer.Count % _param.LengthSamples;
            var prevBufferLen = Math.Min(_pointer, _param.KeepSamples + length);
            var start = _pointer - prevBufferLen;
            var totalLength = size + prevBufferLen;
            
            // move pointer to the current end for next iterations
            _pointer = _buffer.Count - 1;

            // start transcribing sliding window content
            var slice = new ArraySegment<float>(_buffer.ToArray(), start, totalLength);
            _task = _wrapper.GetTextAsync(slice.ToArray(), _param.Frequency, 
                _param.Channels, _param.InferenceParam);
            
            // append transcription to previous result
            var res = await _task;
            _currentSegment = res.Result;
            
            // send update to user
            OnResultUpdated?.Invoke(_output + _currentSegment);

            _step++;
            if (_step >= _param.StepsCount)
            {
                _output += _currentSegment;
                _step = 0;
            }
            
            // update prompt with latest transcription
            if (_param.UpdatePrompt)
            {
                _param.InferenceParam.InitialPrompt = _originalPrompt + _output;
            }


            // reset if its last call
            if (lastCall)
                Reset();
        }

        private void Reset()
        {
            _output = "";
            _currentSegment = "";
            _buffer.Clear();
            _pointer = 0;
            _step = 0;
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