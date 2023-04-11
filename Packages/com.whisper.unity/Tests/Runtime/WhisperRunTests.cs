using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Whisper.Tests
{
    public class WhisperRunTests
    {
        private readonly string _modelPath = Path.Combine(Application.streamingAssetsPath, "Whisper/ggml-tiny.bin");
        private readonly float[] _buffer = new float[32000];
        private const int Frequency = 8000;
        private const int Channels = 2;

        private WhisperWrapper _whisper;
        private WhisperParams _params;
        
        [SetUp]
        public void Setup()
        {
            _whisper = WhisperWrapper.InitFromFile(_modelPath);
            _params = WhisperParams.GetDefaultParams();
        }
        
        [Test]
        public void GetTextTest()
        {
            var res = _whisper.GetText(_buffer, Frequency, Channels, _params);
            Assert.NotNull(res);

            var clip = AudioClip.Create("test", _buffer.Length, Channels, Frequency, false);
            res = _whisper.GetText(clip, _params);
            Assert.NotNull(res);
        }
        
        [Test]
        public async Task GetTextTestAsync()
        {
            var res = await _whisper.GetTextAsync(_buffer, Frequency, Channels, _params);
            Assert.NotNull(res);
            
            var clip = AudioClip.Create("test", _buffer.Length, Channels, Frequency, false);
            res = await _whisper.GetTextAsync(clip, _params);
            Assert.NotNull(res);
        }
        
        [Test]
        public void GetTextTestNonThreadSafeAsync()
        {
            var work1 = new Task(() => _whisper.GetText(_buffer, Frequency, Channels, _params));
            work1.Start();
            var work2 = new Task(() => _whisper.GetText(_buffer, Frequency, Channels, _params));
            work2.Start();

            work1.Wait();
            work2.Wait();
        }
    }
}