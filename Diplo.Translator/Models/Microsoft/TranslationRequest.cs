using System.Text.Json.Serialization;

namespace Diplo.Translator.Models.Microsoft
{
    /// <summary>
    /// Represents a translation request
    /// </summary>
    public class TranslationRequest
    {
        public TranslationRequest()
        {
        }

        /// <summary>
        /// Sets the text to be translated
        /// </summary>
        /// <param name="text">The text</param>
        public TranslationRequest(string text)
        {
            this.Text = text;
        }

        /// <summary>
        /// Get or set the text to be translated
        /// </summary>
        [JsonPropertyName("Text")]
        public string Text { get; set; }

        public override string ToString() => this.Text ?? "[Empty]";
    }
}
