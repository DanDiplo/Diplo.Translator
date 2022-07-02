using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Diplo.Translator.Models.Microsoft
{
    /// <summary>
    /// The result(s) of a translation request
    /// </summary>
    public class TranslationResult
    {
        /// <summary>
        /// Get the language that was detected. If you explicitly set the language in "from" this will be NULL.
        /// </summary>
        [JsonPropertyName("detectedLanguage")]
        public DetectedLanguage DetectedLanguage { get; set; }

        /// <summary>
        /// Get the translation(s)
        /// </summary>
        [JsonPropertyName("translations")]
        public List<Translation> Translations { get; set; }
    }
}
