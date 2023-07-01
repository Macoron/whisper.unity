using System.Collections.Generic;
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
    
    public struct WhisperStreamParams
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

        public WhisperStreamParams(WhisperStreamStrategy strategy, 
            WhisperParams inferenceParam, int frequency, int channels,
            float stepSec = 3f)
        {
            Strategy = strategy;
            InferenceParam = inferenceParam;
            Frequency = frequency;
            Channels = channels;
            
            StepSec = stepSec;
            StepSamples = (int) (stepSec * frequency * channels);
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
        private readonly MicrophoneRecord _microphone;
        
        private readonly List<float> _buffer = new List<float>();

        private int _header;
        private Task<WhisperResult> _task;

        private int _pointer;

        public WhisperStream(WhisperWrapper wrapper, WhisperStreamParams param,
            MicrophoneRecord microphone = null)
        {
            _wrapper = wrapper;
            _param = param;
            
            if (microphone != null)
            {
                _microphone = microphone;
                _microphone.OnChunkReady += MicrophoneOnChunkReady;
                _microphone.OnRecordStop += MicrophoneOnRecordStop;
            }
        }

        private void MicrophoneOnChunkReady(AudioChunk chunk)
        {
            AddToStream(chunk.Data);
        }
        
        private void MicrophoneOnRecordStop(float[] data, int frequency, int channels, float length)
        {
            StopStream();
        }

        public void AddToStream(float[] samples)
        {
            // add new samples to buffer
            _buffer.AddRange(samples);
            
            // call update function
            UpdateRecurrent();
        }

        public async Task StopStream()
        {
            FinishRecurrent();
            _buffer.Clear();
        }

        private async void FinishRecurrent()
        {
            await _task;
            UpdateRecurrent();
        }
        
        private async void UpdateRecurrent()
        {
            // check if task isn't busy
            if (_task != null && !_task.IsCompleted)
                return;
            
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
    }
}