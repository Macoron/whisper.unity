using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Whisper.Utils;

namespace Whisper.Tests
{
    // Testing loading network in different ways
    public class WhisperInitTests
    {
        private readonly string _modelPath = Path.Combine(Application.streamingAssetsPath, "Whisper/ggml-tiny.bin");
    
        [Test]
        public void InitFromFileTest()
        {
            var whisper = WhisperWrapper.InitFromFile(_modelPath);
            Assert.NotNull(whisper);
        }
    
        [Test]
        public async Task InitFromFileAsyncTest()
        {
            var whisper = await WhisperWrapper.InitFromFileAsync(_modelPath);
            Assert.NotNull(whisper);
        }
    
        [Test]
        public void InitFromFileEmptyTest()
        {
            LogAssert.ignoreFailingMessages = true;
            
            var whisper = WhisperWrapper.InitFromFile(null);
            Assert.Null(whisper);
            whisper = WhisperWrapper.InitFromFile("");
            Assert.Null(whisper);
        }
        
        [Test]
        public void InitFromFileDoesntExistTest()
        {
            LogAssert.ignoreFailingMessages = true;
            var whisper = WhisperWrapper.InitFromFile("not/existing/path.bin");
            Assert.Null(whisper);
        }
        
        [Test]
        public void InitFromBufferTest()
        {
            var file = FileUtils.ReadFile(_modelPath);
            Assert.IsNotEmpty(file);
            var whisper = WhisperWrapper.InitFromBuffer(file);
            Assert.NotNull(whisper);
            
        }
    
        [Test]
        public async Task InitFromBufferAsyncTest()
        {
            var file = await FileUtils.ReadFileAsync(_modelPath);
            Assert.IsNotEmpty(file);
            var whisper = await WhisperWrapper.InitFromBufferAsync(file);
            Assert.NotNull(whisper);
        }
        
        [Test]
        public void InitFromEmptyBufferTest()
        {
            LogAssert.ignoreFailingMessages = true;
            var whisper = WhisperWrapper.InitFromBuffer(null);
            Assert.Null(whisper);
            whisper = WhisperWrapper.InitFromBuffer(Array.Empty<byte>());
            Assert.Null(whisper);
        }
        
        [Test]
        public void InitFromInvalidBufferTest()
        {
            LogAssert.ignoreFailingMessages = true;
            var testBuffer = new byte[] { 1, 2, 3 };
            var whisper = WhisperWrapper.InitFromBuffer(testBuffer);
            Assert.Null(whisper);
        }
    }
}

