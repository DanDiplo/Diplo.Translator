using Diplo.Translator.Models;
using Umbraco.Cms.Core.Models;

namespace Diplo.Translator.Services;

public interface IDictionaryTranslationService
{
    IReadOnlyCollection<IDictionaryItem> GetAllDictionaryItems();

    IReadOnlyCollection<ILanguage> GetAllLanguages();

    Task<DictionaryTranslationResponse> TranslateDictionaryAsync(TranslationJob job, CancellationToken cancellationToken = default);
}
