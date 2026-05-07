using Diplo.Translator.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Diplo.Translator.Services;

public sealed class DictionaryTranslationService : IDictionaryTranslationService
{
    private readonly ITranslationService translationService;
    private readonly IDictionaryItemService dictionaryItemService;
    private readonly ILanguageService languageService;
    private readonly ILogger<DictionaryTranslationService> logger;

    public DictionaryTranslationService(
        IDictionaryItemService dictionaryItemService,
        ILanguageService languageService,
        ITranslationService translationService,
        ILogger<DictionaryTranslationService> logger)
    {
        this.dictionaryItemService = dictionaryItemService;
        this.languageService = languageService;
        this.translationService = translationService;
        this.logger = logger;
    }

    public async Task<IReadOnlyCollection<IDictionaryItem>> GetAllDictionaryItemsAsync()
    {
        var dictionaryItems = (await dictionaryItemService.GetAtRootAsync()).ToList();

        foreach (var rootItem in dictionaryItems.ToArray())
        {
            dictionaryItems.AddRange(await dictionaryItemService.GetDescendantsAsync(rootItem.Key, string.Empty));
        }

        return dictionaryItems;
    }

    public async Task<IReadOnlyCollection<ILanguage>> GetAllLanguagesAsync()
        => (await languageService.GetAllAsync()).ToList();

    public async Task<DictionaryTranslationResponse> TranslateDictionaryAsync(TranslationJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var response = new DictionaryTranslationResponse();
        var languages = await GetAllLanguagesAsync();
        var fromLanguage = languages.FirstOrDefault(x => x.IsoCode.InvariantEquals(job.FromCulture)) ?? throw new InvalidOperationException($"Language '{job.FromCulture}' was not found.");
        var targetLanguages = languages.Where(lang => lang.Id != fromLanguage.Id).ToList();
        var sourceCulture = GetCultureName(fromLanguage);
        var dictionaryItems = await GetAllDictionaryItemsAsync();

        response.DictionaryCount = dictionaryItems.Count;
        job.Progress.DictionaryCount = dictionaryItems.Count;
        job.SetStatus(TranslationJobStatus.Running, "Translating dictionary items...");

        foreach (var entry in dictionaryItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            job.Progress.ProcessedItemCount++;
            job.SetCurrentItem(entry.ItemKey);
            var translatedCultures = new List<string>();
            var fromText = GetTranslationValue(entry, fromLanguage.IsoCode);

            if (string.IsNullOrWhiteSpace(fromText))
            {
                AddSkipped(response, job, targetLanguages.Count);
                continue;
            }

            foreach (var lang in targetLanguages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var existingValue = GetTranslationValue(entry, lang.IsoCode);

                if (!string.IsNullOrWhiteSpace(existingValue) && !job.Overwrite)
                {
                    AddSkipped(response, job);
                    continue;
                }

                var targetCulture = GetCultureName(lang);

                var translatedText = await TranslateDictionaryItemAsync(
                    fromText,
                    targetCulture,
                    sourceCulture,
                    job,
                    response,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(translatedText))
                {
                    continue;
                }

                entry.AddOrUpdateDictionaryValue(lang, translatedText);
                translatedCultures.Add(targetCulture);
            }

            if (translatedCultures.Count is 0)
            {
                continue;
            }

            await SaveDictionaryItemAsync(entry, translatedCultures, job, response);
        }

        return response;
    }

    private static string GetCultureName(ILanguage language)
        => language.CultureInfo?.Name ?? language.IsoCode;

    private static string? GetTranslationValue(IDictionaryItem entry, string isoCode)
        => entry.Translations.FirstOrDefault(x => x.LanguageIsoCode.InvariantEquals(isoCode))?.Value;

    private static void AddSkipped(DictionaryTranslationResponse response, TranslationJob job, int count = 1)
    {
        if (count <= 0)
        {
            return;
        }

        response.SkippedCount += count;
        job.Progress.SkippedCount += count;
    }

    private async Task SaveDictionaryItemAsync(
        IDictionaryItem entry,
        IReadOnlyCollection<string> translatedCultures,
        TranslationJob job,
        DictionaryTranslationResponse response)
    {
        try
        {
            var result = await dictionaryItemService.UpdateAsync(entry, job.UserKey);

            if (result.Success is false)
            {
                throw new InvalidOperationException($"Dictionary item update failed with status '{result.Status}'.");
            }

            job.AddMessage($"Translated '{entry.ItemKey}' to {string.Join(", ", translatedCultures)}.");
        }
        catch (Exception ex)
        {
            var message = $"Error saving '{entry.ItemKey}'.";
            logger.LogError(ex, message);
            job.AddError(message);
            response.ErrorCount++;
            job.Progress.ErrorCount++;
        }
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
