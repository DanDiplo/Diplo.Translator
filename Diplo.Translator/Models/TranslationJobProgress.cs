namespace Diplo.Translator.Models;

public sealed class TranslationJobProgress
{
    public int DictionaryCount { get; set; }

    public int ProcessedItemCount { get; set; }

    public int AttemptedTranslationCount { get; set; }

    public int TranslationCount { get; set; }

    public int ErrorCount { get; set; }

    public int SkippedCount { get; set; }

    public string? CurrentItem { get; set; }

    public string Message => $"Translated {TranslationCount} out of {DictionaryCount} items with {ErrorCount} errors";

    public TranslationJobProgress Clone()
        => new()
        {
            DictionaryCount = DictionaryCount,
            ProcessedItemCount = ProcessedItemCount,
            AttemptedTranslationCount = AttemptedTranslationCount,
            TranslationCount = TranslationCount,
            ErrorCount = ErrorCount,
            SkippedCount = SkippedCount,
            CurrentItem = CurrentItem
        };
}
