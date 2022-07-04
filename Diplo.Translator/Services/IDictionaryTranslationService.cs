using Diplo.Translator.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;

namespace Diplo.Translator.Services
{
    public interface IDictionaryTranslationService
    {
        IEnumerable<IDictionaryItem> GetAllDictionaryItems();

        IEnumerable<ILanguage> GetAllLanguages();

        Task<DictionaryTranslationResponse> TranslateDictionary(string clientId, string fromCulture = null, bool overwrite = false);
    }
}