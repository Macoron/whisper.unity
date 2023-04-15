using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Whisper.Samples
{
    public class LanguagesDemo : MonoBehaviour
    {
        public WhisperManager whisper;
        public Text text;

        private async void Awake()
        {
            await whisper.InitModel();
            UpdateText();
        }
        
        private void UpdateText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("All languages names:");

            // write all languages in one string
            var languages = WhisperLanguage.GetAllLanguages();
            for (var i = 0; i < languages.Length - 1; i++)
                sb.Append(languages[i] + ", ");
            sb.Append(languages[languages.Length - 1] + ".\n\n");

            // check if Multilingual
            var multi = whisper.IsMultilingual();
            var msg = "Current model Multilingual: " + multi;
            sb.AppendLine(msg);

            text.text = sb.ToString();
        }
    } 
}

