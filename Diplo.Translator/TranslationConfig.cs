namespace Diplo.Translator
{
    /// <summary>
    /// Configuration settings
    /// </summary>
    public class TranslationConfig
    {
        /// <summary>
        /// The name in appSettings
        /// </summary>
        public const string ConfigSectionName = "Diplo.Translator";

        /// <summary>
        /// Get or set the URL of the translator API
        /// </summary>
        public string TranslatorApiEndpoint { get; set; }

        /// <summary>
        /// Get or set the API key used by the translation service
        /// </summary>
        public string TranslatorApiKey { get; set; }

        /// <summary>
        /// Get or set the name of the translation service (currently just MS)
        /// </summary>
        public string TranslatorService { get; set; } = "Microsoft";

        /// <summary>
        /// Get or set the API version number
        /// </summary>
        public string TranslatorApiVersion { get; set; } = "3.0";

        /// <summary>
        /// Gets whether the settings are configured (or at least not empty!)
        /// </summary>
        /// <returns>True if they are; otherwise false</returns>
        public bool IsConfigured() => !string.IsNullOrEmpty(TranslatorApiEndpoint) && !string.IsNullOrEmpty(TranslatorApiKey);
    }
}
