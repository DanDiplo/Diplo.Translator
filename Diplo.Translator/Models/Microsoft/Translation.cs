

using System.Text.Json.Serialization;

namespace Diplo.Translator.Models.Microsoft
{
    /// <summary>
    /// Represents a translated item
    /// </summary>
    public class Translation
    {
        /// <summary>
        /// Get the translated text
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }

        /// <summary>
        /// Get the language translated to
        /// </summary>
        [JsonPropertyName("to")]
        public string To { get; set; }

        public override string ToString() => $"{To}: '{Text}'";
    }
}
