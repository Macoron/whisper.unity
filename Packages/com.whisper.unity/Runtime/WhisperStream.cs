using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Whisper.Utils;
// ReSharper disable once RedundantUsingDirective
using System.Linq;

namespace Whisper
{
    public delegate void OnStreamResultUpdatedDelegate(string updatedResult);
    public delegate void OnStreamSegmentUpdatedDelegate(WhisperResult segment);   
    public delegate void OnStreamSegmentFinishedDelegate(WhisperResult segment);   
    public delegate void OnStreamFinishedDelegate(string finalResult);
    
    /// <summary>
    /// Parameters of whisper streaming processing.
    /// </summary>
    public class WhisperStreamParams
    {
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

        /// <summary>
        /// How many seconds of previous segment will be used for current segment.
        /// </summary>
        public readonly float KeepSec;

        /// <summary>
        /// How many samples of previous audio chunk will be used for current chunk.
        /// </summary>
        public readonly int KeepSamples;

        /// <summary>
        /// How many seconds of audio will be recurrently transcribe until context update.
        /// </summary>
        public readonly float LengthSec;
        
        /// <summary>
        /// How many samples of audio will be recurrently transcribe until context update.
        /// </summary>
        public readonly int LengthSamples;

        /// <summary>
        /// Should stream modify whisper prompt for better context handling?
        /// </summary>
        public readonly bool UpdatePrompt;

        /// <summary>
        /// How many recurrent iterations will be used for one chunk?
        /// </summary>
        public readonly int StepsCount;

        /// <summary>
        /// If false stream will use all information from previous iteration.
        /// </summary>
        public readonly bool DropOldBuffer;

        /// <summary>
        /// If true stream will ignore audio chunks with no detected speech.
        /// </summary>
        public readonly bool UseVad;

        public WhisperStreamParams(WhisperParams inferenceParam,
            int frequency, int channels,
            float stepSec = 3f, float keepSec = 0.2f, float lengthSec = 10f,
            bool updatePrompt = true, bool dropOldBuffer = false,
            bool useVad = false)
        {
            InferenceParam = inferenceParam;
            Frequency = frequency;
            Channels = channels;
            
            StepSec = stepSec;
            StepSamples = (int) (StepSec * Frequency * Channels);

            KeepSec = keepSec;
            KeepSamples = (int) (KeepSec * frequency * channels);

            LengthSec = lengthSec;
            LengthSamples = (int) (LengthSec * frequency * channels);

            StepsCount = Math.Max(1, (int) (LengthSec / StepSec) - 1);
            
            UpdatePrompt = updatePrompt;
            DropOldBuffer = dropOldBuffer;
            UseVad = useVad;
        }
    }
    
    /// <summary>
    /// Handling all streaming logic (sliding-window, VAD, etc).
    /// </summary>
    public class WhisperStream
    {
        /// <summary>
        /// Raised when whisper updated stream transcription.
        /// Result contains a full stream transcript from the stream beginning.
        /// </summary>
        public event OnStreamResultUpdatedDelegate OnResultUpdated;
        /// <summary>
        /// Raised when whisper updated current segment transcript.
        /// </summary>
        public event OnStreamSegmentUpdatedDelegate OnSegmentUpdated;
        /// <summary>
        /// Raised when whisper finished current segment transcript. 
        /// </summary>
        public event OnStreamSegmentFinishedDelegate OnSegmentFinished;
        /// <summary>
        /// Raised when whisper finished stream transcription and can start another one.
        /// </summary>
        public event OnStreamFinishedDelegate OnStreamFinished;
        
        private readonly WhisperWrapper _wrapper;
        private readonly WhisperStreamParams _param;
        private readonly string _originalPrompt;
        private readonly MicrophoneRecord _microphone;
        
        private readonly List<float> _newBuffer = new List<float>();
        private float[] _oldBuffer = Array.Empty<float>();
        private string _output = "";
        private int _step;
        private bool _isStreaming;
        
        private Task<WhisperResult> _task;

        /// <summary>
        /// Create a new instance of Whisper streaming transcription.
        /// </summary>
        /// <param name="wrapper">Loaded Whisper model which will be used for transcription.</param>
        /// <param name="param">Whisper streaming parameters.</param>
        /// <param name="microphone">Optional microphone input for stream.</param>
        public WhisperStream(WhisperWrapper wrapper, WhisperStreamParams param,
            MicrophoneRecord microphone = null)
        {
            _wrapper = wrapper;
            _param = param;
            _originalPrompt = _param.InferenceParam.InitialPrompt;
            _microphone = microphone;
        }
        
        /// <summary>
        /// Start a new streaming transcription. Must be called before
        /// you start adding new audio chunks.
        /// </summary>
        /// <remarks>
        /// If you set microphone into constructor, it will start listening to it.
        /// Make sure you started microphone by <see cref="MicrophoneRecord.StartRecord"/>.
        /// There is no need to add audio chunks manually using <see cref="AddToStream"/>.
        /// </remarks>
        public void StartStream()
        {
            if (_isStreaming)
            {
                LogUtils.Warning("Stream is already working!");
                return;
            }
            _isStreaming = true;
            
            // if we set microphone - streaming works in auto mode
            if (_microphone != null)
            {
                _microphone.OnChunkReady += MicrophoneOnChunkReady;
                _microphone.OnRecordStop += MicrophoneOnRecordStop;
            }
        }

        /// <summary>
        /// Manually add a new chunk of audio to streaming.
        /// Make sure to call <see cref="StartStream"/> first.
        /// </summary>
        /// <remarks>
        /// If you set microphone into constructor, it will be called automatically.
        /// </remarks>
        public async void AddToStream(AudioChunk chunk)
        {
            if (!_isStreaming)
            {
                LogUtils.Warning("Start streaming first!");
                return;
            }

            if (_param.UseVad)
            {
                if (chunk.IsVoiceDetected)
                {
                    _newBuffer.AddRange(chunk.Data);
                    await UpdateSlidingWindow();
                }
                else
                {
                    if (_step <= 0)
                    {
                        _oldBuffer = chunk.Data;
                        return;
                    }

                    _newBuffer.AddRange(chunk.Data);
                    await UpdateSlidingWindow(true);
                }
            }
            else
            {
                _newBuffer.AddRange(chunk.Data);
                await UpdateSlidingWindow();
            }
        }

        /// <summary>
        /// Stop current streaming transcription. It will process last
        /// audio chunks and raise <see cref="OnStreamFinished"/> when it's done.
        /// </summary>
        public async void StopStream()
        {
            if (!_isStreaming)
            {
                LogUtils.Warning("Start streaming first!");
                return;
            }
            _isStreaming = false;
            
            // unsubscribe from microphone events for now
            if (_microphone != null)
            {
                _microphone.OnChunkReady -= MicrophoneOnChunkReady;
                _microphone.OnRecordStop -= MicrophoneOnRecordStop;
            }
            
            // first wait until last task complete
            if (_task != null)
                await _task;
            
            // finish last part
            await UpdateSlidingWindow(true);
            OnStreamFinished?.Invoke(_output);
            
            // reset stream and drop audio buffer
            Reset();
        }
        
        private async Task UpdateSlidingWindow(bool forceSegmentEnd = false)
        {
            // check if task isn't busy
            // if it's still transcribing - just skip it
            // next iteration will handle current and future data
            if (_task != null && !_task.IsCompleted)
                return;
            
            // check if we have enough data to start transcribing
            // if we need to finish segment now - just grab all whats left
            var newBufferLen = _newBuffer.Count;
            if (!forceSegmentEnd && newBufferLen < _param.StepSamples)
                return;

            // calculate how much we can get from _oldBuffer
            var oldBufferLen = _oldBuffer.Length;
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
            Array.Copy(_oldBuffer, oldBufferStart, 
                buffer, 0, nSamplesTake);

            // and now add data from new buffer
            _newBuffer.CopyTo(0, buffer, nSamplesTake, newBufferLen);

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

            // send update to user
            OnSegmentUpdated?.Invoke(res);
            OnResultUpdated?.Invoke(currentOutput);
            
            // check if finished working on current chunk
            // TODO: when VAD active divide only by silence?
            _step++;
            if (forceSegmentEnd || _step >= _param.StepsCount)
            {
                LogUtils.Verbose($"Stream finished an old segment with total steps of {_step}");
                _output = currentOutput;

                // TODO: don't use string prompt - use tokenized prompt_tokens
                // update prompt with latest transcription
                if (_param.UpdatePrompt)
                    _param.InferenceParam.InitialPrompt = _originalPrompt + _output;

                // trim old buffer
                var updBufferLen = _param.KeepSamples;
                if (updBufferLen > bufferLen)
                    updBufferLen = bufferLen;

                var segment = new ArraySegment<float>(buffer, bufferLen - updBufferLen, updBufferLen);
                _oldBuffer = segment.ToArray();
                _step = 0;
                
                OnSegmentFinished?.Invoke(res);
            }
            else
            {
                LogUtils.Verbose("Stream continues current segment");
                // swap buffers
                _oldBuffer = buffer;
            }
        }

        private void Reset()
        {
            _output = "";
            _step = 0;
            _oldBuffer = Array.Empty<float>();
            _newBuffer.Clear();
        }
        
        private void MicrophoneOnChunkReady(AudioChunk chunk)
        {
            AddToStream(chunk);
        }
        
        private void MicrophoneOnRecordStop(AudioChunk recordedAudio)
        {
            StopStream();
        }

    }
}