using System.Text.Json.Serialization;

namespace Diplo.Translator.Models.Microsoft
{
    /// <summary>
    /// Represents the language detected
    /// </summary>
    public class DetectedLanguage
    {
        /// <summary>
        /// The language code
        /// </summary>
        [JsonPropertyName("language")]
        public string Language { get; set; }

        /// <summary>
        /// The confidence score regarding the language
        /// </summary>
        [JsonPropertyName("score")]
        public double Score { get; set; }

        public override string ToString() => $"{Language} ({Score} confidence)";
    }
}
