using NUnit.Framework;
using UnityEngine;
using Whisper.Utils;

namespace Whisper.Tests
{
    public class AudioUtilsTests
    {
        [Test]
        public void ConvertToMonoTest()
        {
            // create stereo audio
            // left is filled with 0, right with 2
            const int samplesCount = 200;
            var samples = new float[samplesCount];
            for (var i = 0; i < samplesCount; i++)
                samples[i] = i % 2 == 0 ? 2 : 0;
            
            // convert to mono
            var mono = AudioUtils.ConvertToMono(samples, 2);
            
            // check if length correct
            Assert.AreEqual(samplesCount/2, mono.Length);
            
            // check if get expected value (average of both channels)
            foreach (var i in mono)
                Assert.That(Mathf.Approximately(i, 1f));
        }

        [Test]
        public void ChangeSampleRateTest()
        {
            // create mono audio of 5 sec length
            const int time = 5;
            const int srcRate = 16000;
            var src = new float[srcRate * time];

            // upscale 
            var dstRate = 32000;
            var dst = AudioUtils.ChangeSampleRate(src, srcRate, dstRate);
            Assert.AreEqual(dstRate * time, dst.Length);
            
            // downscale
            dstRate = 8000;
            dst = AudioUtils.ChangeSampleRate(src, srcRate, dstRate);
            Assert.AreEqual(dstRate * time, dst.Length);
        }
    }
}