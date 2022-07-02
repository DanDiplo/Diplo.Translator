namespace Diplo.Translator
{
    /// <summary>
    /// Configuration settings
    /// </summary>
    public class TranslationConfig
    {
        public const string ConfigSectionName = "Diplo.Translator";

        public string TranslatorApiEndpoint { get; set; }

        public string TranslatorApiKey { get; set; }

        public string TranslatorService { get; set; } = "Microsoft";

        public string TranslatorApiVersion { get; set; } = "3.0";

        public bool IsConfigured() => !string.IsNullOrEmpty(TranslatorApiEndpoint) && !string.IsNullOrEmpty(TranslatorApiKey);
    }
}
