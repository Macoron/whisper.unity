using NUnit.Framework;

namespace Whisper.Tests
{
    public class WhisperLanguageTests
    {
        [Test]
        public void GetLanguageMaxIdTest()
        {
            var maxId = WhisperLanguage.GetLanguageMaxId();
            Assert.GreaterOrEqual(maxId, 0);
        }
        
        [Test]
        public void GetLanguageIdTest()
        {
            // basic
            var id = WhisperLanguage.GetLanguageId("en");
            Assert.AreEqual(0, id);
            id = WhisperLanguage.GetLanguageId("english");
            Assert.AreEqual(0, id);
            
            // non-existing
            id = WhisperLanguage.GetLanguageId("klingon");
            Assert.AreEqual(-1, id);
            
            // invalid 
            id = WhisperLanguage.GetLanguageId(null);
            Assert.AreEqual(-1, id);
            id = WhisperLanguage.GetLanguageId("");
            Assert.AreEqual(-1, id);
        }

        [Test]
        public void GetLanguageStringTest()
        {
            // basic
            var str = WhisperLanguage.GetLanguageString(0);
            Assert.AreEqual("en", str);
            
            var maxId = WhisperLanguage.GetLanguageMaxId();
            str = WhisperLanguage.GetLanguageString(maxId);
            Assert.NotNull(str);
            
            // invalid
            str = WhisperLanguage.GetLanguageString(maxId + 1);
            Assert.Null(str);
            str = WhisperLanguage.GetLanguageString(-1);
            Assert.Null(str);
        }

        [Test]
        public void GetAllLanguagesTest()
        {
            var languages = WhisperLanguage.GetAllLanguages();
            var expectedLength = WhisperLanguage.GetLanguageMaxId() + 1;
            
            // sanity checks
            Assert.That(languages != null);
            Assert.AreEqual(expectedLength, languages.Length);

            var lang = WhisperLanguage.GetLanguageString(0);
            Assert.AreEqual(lang, languages[0]);
        }
    }
}