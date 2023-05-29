using NUnit.Framework;
using Whisper.Native;

namespace Whisper.Tests
{
    [TestFixture]
    public class WhisperParamsTests
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

        [Test]
        public void PromptParamsTest()
        {
            var param = WhisperParams.GetDefaultParams();
            Assert.NotNull(param);
            
            // check get default prompt
            Assert.DoesNotThrow(() => { var tmp = param.InitialPrompt; });
            
            // check no prompt provided
            param.InitialPrompt = "";
            Assert.AreEqual("", param.InitialPrompt);
            param.InitialPrompt = null;
            Assert.AreEqual(null, param.InitialPrompt);
            
            // check prompt changing
            const string constPrompt = "hello how is it going always use lowercase no punctuation goodbye one two three start stop i you me they";
            param.InitialPrompt = constPrompt;
            Assert.AreEqual(constPrompt, param.InitialPrompt);
        }
    }
}