using Diplo.Translator.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Diplo.Translator.Services;

public sealed class DictionaryTranslationService : IDictionaryTranslationService
{
    private readonly ITranslationService translationService;
    private readonly ILocalizationService localizationService;
    private readonly ILogger<DictionaryTranslationService> logger;

    public DictionaryTranslationService(
        ILocalizationService localizationService,
        ITranslationService translationService,
        ILogger<DictionaryTranslationService> logger)
    {
        this.localizationService = localizationService;
        this.translationService = translationService;
        this.logger = logger;
    }

    public IReadOnlyCollection<IDictionaryItem> GetAllDictionaryItems()
    {
        var rootDictionary = localizationService.GetRootDictionaryItems().ToList();
        return rootDictionary
            .Concat(rootDictionary.SelectMany(r => localizationService.GetDictionaryItemDescendants(r.Key)))
            .ToList();
    }

    public IReadOnlyCollection<ILanguage> GetAllLanguages()
        => localizationService.GetAllLanguages().ToList();

    public async Task<DictionaryTranslationResponse> TranslateDictionaryAsync(TranslationJob job, CancellationToken cancellationToken = default)
    {
        var response = new DictionaryTranslationResponse();
        var languages = GetAllLanguages();
        var fromLanguage = languages.FirstOrDefault(x => x.IsoCode.InvariantEquals(job.FromCulture));

        if (fromLanguage is null)
        {
            throw new InvalidOperationException($"Language '{job.FromCulture}' was not found.");
        }

        var dictionaryItems = GetAllDictionaryItems();
        response.DictionaryCount = dictionaryItems.Count;
        job.Progress.DictionaryCount = dictionaryItems.Count;
        job.SetStatus(TranslationJobStatus.Running, "Translating dictionary items...");

        foreach (var entry in dictionaryItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            job.Progress.ProcessedItemCount++;
            job.SetCurrentItem(entry.ItemKey);

            foreach (var lang in languages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (lang.Id == fromLanguage.Id)
                {
                    continue;
                }

                var translation = entry.Translations.FirstOrDefault(x => x.LanguageIsoCode.InvariantEquals(lang.IsoCode));
                var fromTranslation = entry.Translations.FirstOrDefault(x => x.LanguageIsoCode.InvariantEquals(fromLanguage.IsoCode));

                if (string.IsNullOrWhiteSpace(fromTranslation?.Value))
                {
                    response.SkippedCount++;
                    job.Progress.SkippedCount++;
                    continue;
                }

                if (translation is not null && !string.IsNullOrWhiteSpace(translation.Value) && !job.Overwrite)
                {
                    response.SkippedCount++;
                    job.Progress.SkippedCount++;
                    continue;
                }

                    var targetCulture = lang.CultureInfo?.Name ?? lang.IsoCode;
                    var sourceCulture = fromLanguage.CultureInfo?.Name ?? fromLanguage.IsoCode;

                    var translatedText = await TranslateDictionaryItemAsync(
                        fromTranslation.Value,
                        targetCulture,
                        sourceCulture,
                        job,
                        response,
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(translatedText))
                {
                    continue;
                }

                try
                {
                    localizationService.AddOrUpdateDictionaryValue(entry, lang, translatedText);
                    localizationService.Save(entry);
                    job.AddMessage($"Translated '{entry.ItemKey}' to {targetCulture}.");
                }
                catch (Exception ex)
                {
                    var message = $"Error saving '{entry.ItemKey}' for {targetCulture}.";
                    logger.LogError(ex, message);
                    job.AddError(message);
                    response.ErrorCount++;
                    job.Progress.ErrorCount++;
                }
            }
        }

        return response;
    }

    private async Task<string?> TranslateDictionaryItemAsync(
        string fromText,
        string to,
        string from,
        TranslationJob job,
        DictionaryTranslationResponse response,
        CancellationToken cancellationToken)
    {
        job.Progress.AttemptedTranslationCount++;
        job.SetStatus(TranslationJobStatus.Running, $"Translating {from} to {to}...");

        var result = await translationService.TranslateAsync(fromText, to, from, cancellationToken);

        if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Text))
        {
            response.TranslationCount++;
            job.Progress.TranslationCount++;
            return result.Text;
        }

        var message = string.IsNullOrWhiteSpace(result.Message)
            ? $"Translation from {from} to {to} returned no text."
            : result.Message;

        logger.LogError("{TranslationError}", message);
        job.AddError(message!);
        response.ErrorCount++;
        job.Progress.ErrorCount++;
        return null;
    }
}
