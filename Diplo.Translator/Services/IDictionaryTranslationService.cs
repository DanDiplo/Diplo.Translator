using Diplo.Translator.Models;
using Umbraco.Cms.Core.Models;

namespace Diplo.Translator.Services;

public interface IDictionaryTranslationService
{
    Task<IReadOnlyCollection<IDictionaryItem>> GetAllDictionaryItemsAsync();

    Task<IReadOnlyCollection<ILanguage>> GetAllLanguagesAsync();

    Task<DictionaryTranslationResponse> TranslateDictionaryAsync(TranslationJob job, CancellationToken cancellationToken = default);
}
