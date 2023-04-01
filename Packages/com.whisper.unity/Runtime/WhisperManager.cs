using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Whisper
{
    public class WhisperManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Path to model weights file relative to StreamingAssets")]
        private string modelPath = "Whisper/ggml-base.bin";
        
        [SerializeField]
        [Tooltip("Should model weights be loaded on awake?")]
        private bool initOnAwake = true;
    
        [SerializeField]
        [Tooltip("Output text language. Use \"auto\" for auto-detection.")]
        private string language = "en";
        
        private WhisperWrapper _whisper;
        private WhisperParams _params;

        public bool IsLoaded => _whisper != null;
        public bool IsLoading { get; private set; }
        public bool IsBusy { get; private set; }
        
        private async void Awake()
        {
            if (!initOnAwake)
                return;
            await InitModel();
        }
        
        /// <summary>
        /// Load model and default parameters. Prepare it for text transcription.
        /// </summary>
        public async Task InitModel()
        {
            // check if model is already loaded or actively loading
            if (IsLoaded)
            {
                Debug.LogWarning("Whisper model is already loaded and ready for use!");
                return;
            } 
            if (IsLoading)
            {
                Debug.LogWarning("Whisper model is already loading!");
                return;
            }
            
            // load model and default params
            IsLoading = true;
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, modelPath);
                _whisper = await WhisperWrapper.InitFromFileAsync(path);
                
                _params = WhisperParams.GetDefaultParams();
                _params.Language = language;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            IsLoading = false;
        }
        
        /// <summary>
        /// Get transcription from audio clip.
        /// </summary>
        public async Task<WhisperResult> GetTextAsync(AudioClip clip)
        {
            var isLoaded = await CheckIfLoaded();
            if (!isLoaded)
                return null;
            
            var res = await _whisper.GetTextAsync(clip, _params);
            return res;
        }
        
        
        /// <summary>
        /// Get transcription from audio buffer.
        /// </summary>
        public async Task<WhisperResult> GetTextAsync(float[] samples, int frequency, int channels)
        {
            var isLoaded = await CheckIfLoaded();
            if (!isLoaded)
                return null;
            
            var res = await _whisper.GetTextAsync(samples, frequency, channels, _params);
            return res;
        }
        
        /// <summary>
        /// Choose text output language.
        /// </summary>
        public void SetLanguage(string newLanguage)
        {
            language = newLanguage;
            _params.Language = newLanguage;
        }

        private async Task<bool> CheckIfLoaded()
        {
            if (!IsLoaded && !IsLoading)
            {
                Debug.LogError("Whisper model isn't loaded! Init Whisper model first!");
                return false;
            }
            
            // wait while model still loading
            while (IsLoading)
            {
                await Task.Yield();
            }

            return IsLoaded;
        }
    }
}

