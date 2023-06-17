using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Whisper.Tests
{
    [TestFixture]
    public class WhisperPromptTests
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
        public void RunWithNullPrompt()
        {
            _params.InitialPrompt = null;
            
            var res = _whisper.GetText(_buffer, Frequency, Channels, _params);
            Assert.NotNull(res);
        }
        
        [Test]
        public void RunWithEmptyPrompt()
        {
            _params.InitialPrompt = "";
            
            var res = _whisper.GetText(_buffer, Frequency, Channels, _params);
            Assert.NotNull(res);
        }
        
        [Test]
        public void TextDiffersDueToPrompt()
        {
            var clip = AudioClip.Create("test", _buffer.Length, Channels, Frequency, false);
            
            var res1 = _whisper.GetText(clip, _params);
            Assert.NotNull(res1);

            _params.InitialPrompt = "hello how is it going always use lowercase no punctuation goodbye one two three start stop i you me they" +
                                    " EVERY WORD IS WRITTEN IN CAPITAL LETTERS AS IF THE CAPS LOCK KEY WAS PRESSED" +
                                    ". This long prompt should change the result a lot, i hope!";
            var res2 = _whisper.GetText(clip, _params);
            Assert.NotNull(res2);
            
            Assert.True(res1.Result != res2.Result);
        }
    }
}