namespace Diplo.Translator.Models
{
    /// <summary>
    /// Represents the response returned by the <see cref="Services.DictionaryTranslationService"/>
    /// </summary>
    public class DictionaryTranslationResponse
    {
        /// <summary>
        /// How many items in dictionary
        /// </summary>
        public int DictionaryCount { get; set; }

        /// <summary>
        /// How many items translated
        /// </summary>
        public int TranslationCount { get; set; }

        /// <summary>
        /// How many items errored
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// A message
        /// </summary>
        public string Message => $"Translated {TranslationCount} out of {DictionaryCount} items with {ErrorCount} errors";
    }
}
