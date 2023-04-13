using System.Runtime.InteropServices;
using Whisper.Native;

namespace Whisper
{
    public static class WhisperLanguage
    {
        private static string[] _allLanguages;
        
        /// <summary>
        /// Largest language id (i.e. number of available languages - 1).
        /// </summary>
        public static int GetLanguageMaxId()
        {
            return WhisperNative.whisper_lang_max_id();
        }

        /// <summary>
        /// Return the id of the specified language (e.g. "de" -> 2; "german" -> 2).
        /// Returns -1 if not found.
        /// </summary>
        /// <returns></returns>
        public static int GetLanguageId(string lang)
        {
            if (string.IsNullOrEmpty(lang))
                return -1;
            
            return WhisperNative.whisper_lang_id(lang);
        }

        /// <summary>
        /// Return the short string of the specified language id (e.g. 2 -> "de").
        /// Null if not found.
        /// </summary>
        public static string GetLanguageString(int id)
        {
            var strPtr = WhisperNative.whisper_lang_str(id);
            var str = Marshal.PtrToStringAnsi(strPtr);
            return str;
        }

        /// <summary>
        /// Return all languages strings (e.g. ["en", "de", "ru"]).
        /// </summary>
        /// <remarks>
        /// It doesn't mean that your models weights can work with this language.
        /// This is general list for all whisper.cpp models.
        /// </remarks>
        public static string[] GetAllLanguages()
        {
            if (_allLanguages != null)
                return _allLanguages;

            var count = GetLanguageMaxId() + 1;
            _allLanguages = new string[count];
            for (var i = 0; i < count; i++)
            {
                var lang = GetLanguageString(i);
                _allLanguages[i] = lang;
            }

            return _allLanguages;
        }
    }
}