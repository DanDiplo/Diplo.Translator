namespace Diplo.Translator.Models;

public sealed class TranslationJobRequest
{
    public string FromCulture { get; set; } = string.Empty;

    public bool Overwrite { get; set; }
}
