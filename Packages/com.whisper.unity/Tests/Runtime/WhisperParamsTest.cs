using NUnit.Framework;
using Whisper.Native;

namespace Whisper.Tests
{
    [TestFixture]
    public class WhisperParamsTest
    {
        [Test]
        [TestCase(WhisperSamplingStrategy.WHISPER_SAMPLING_GREEDY)]
        [TestCase(WhisperSamplingStrategy.WHISPER_SAMPLING_BEAM_SEARCH)]
        public void DefaultParamsStrategyTest(WhisperSamplingStrategy strategy)
        {
            var param = WhisperParams.GetDefaultParams(strategy);
            Assert.NotNull(param);
            Assert.AreEqual(param.Strategy, strategy);
        }

        [Test]
        public void LanguageParamsTest()
        {
            var param = WhisperParams.GetDefaultParams();
            Assert.NotNull(param);

            // check default language
            Assert.AreEqual("en", param.Language);
            
            // check auto language
            param.Language = "";
            Assert.AreEqual("", param.Language);
            param.Language = null;
            Assert.AreEqual(null, param.Language);
            
            // check language switch
            param.Language = "de";
            Assert.AreEqual("de", param.Language);
        }
    }
}