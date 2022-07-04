using Diplo.Translator.Hubs;
using Diplo.Translator.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Diplo.Translator.Services
{
    /// <summary>
    /// Service to translate dictionary items and get related data
    /// </summary>
    public class DictionaryTranslationService : IDictionaryTranslationService
    {
        private readonly ITranslationService translationService;
        private readonly ILocalizationService localizationService;
        private readonly ILogger<DictionaryTranslationService> logger;
        private readonly IHubContext<DiploHub> hubContext;
        private IEnumerable<ILanguage> _languages;

        public DictionaryTranslationService(ILocalizationService localizationService, ITranslationService translationService, ILogger<DictionaryTranslationService> logger, IHubContext<DiploHub> hubContext)
        {
            this.localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            this.translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        /// <summary>
        /// Gets all dictionary items from Umbraco
        /// </summary>
        public IEnumerable<IDictionaryItem> GetAllDictionaryItems()
        {
            var rootDictionary = this.localizationService.GetRootDictionaryItems();
            return rootDictionary.Concat(rootDictionary.SelectMany(r => localizationService.GetDictionaryItemDescendants(r.Key)));
        }

        /// <summary>
        /// Gets all configured languages from Umbraco
        /// </summary>
        public IEnumerable<ILanguage> GetAllLanguages()
        {
            if (_languages == null)
            {
                _languages = this.localizationService.GetAllLanguages().ToList();
            }

            return _languages;
        }

        /// <summary>
        /// Performs the translation and outputs to signalR
        /// </summary>
        /// <param name="clientId">The signlaR client ID</param>
        /// <param name="fromCulture">The culture to translate from</param>
        /// <param name="overwrite">Whether to overwrite existing values</param>
        /// <returns>A response object</returns>
        public async Task<DictionaryTranslationResponse> TranslateDictionary(string clientId, string fromCulture = null, bool overwrite = false)
        {
            var response = new DictionaryTranslationResponse();
            var hubClient = new DiploHubClientService(hubContext, clientId);
            await hubClient.SendAlertAsync(new Alert("Translating..."));
            ILanguage fromLanguage = null;

            var languages = this.GetAllLanguages();

            if (!string.IsNullOrEmpty(fromCulture))
            {
                fromLanguage = languages.FirstOrDefault(x => x.IsoCode.InvariantEquals(fromCulture));
            }

            if (fromLanguage == null)
            {
                fromLanguage = languages.First(x => x.IsDefault);
            }

            var dictionaryItems = this.GetAllDictionaryItems();

            response.DictionaryCount = dictionaryItems.Count();

            foreach (var entry in dictionaryItems)
            {
                foreach (var lang in languages)
                {
                    var translation = entry.Translations.FirstOrDefault(x => x.LanguageId == lang.Id);
                    var fromTranslation = entry.Translations.FirstOrDefault(x => x.LanguageId == fromLanguage.Id);

                    if (fromTranslation != null && !string.IsNullOrEmpty(fromTranslation.Value))
                    {
                        if (translation == null || string.IsNullOrEmpty(translation.Value) || overwrite)
                        {
                            string translatedText = await this.TranslateDictionaryItem(fromTranslation.Value, lang.CultureInfo.Name, fromLanguage.CultureInfo.Name, hubClient, response);

                            if (!string.IsNullOrEmpty(translatedText))
                            {
                                try
                                {
                                    this.localizationService.AddOrUpdateDictionaryValue(entry, lang, translatedText);
                                    this.localizationService.Save(entry);
                                    await hubClient.SendAlertAsync(new Alert(AlertType.Success, $"{lang.CultureInfo.Name}: {fromTranslation.Value}' => '{translatedText}"));
                                }
                                catch (Exception ex)
                                {
                                    var message = $"Error translating {lang.CultureInfo.Name}: {fromTranslation.Value}";
                                    logger.LogError(message, ex);
                                    await hubClient.SendAlertAsync(new Alert(AlertType.Error, message));
                                    response.ErrorCount++;
                                }
                            }
                        }
                    }
                }
            }

            return response;
        }

        private async Task<string> TranslateDictionaryItem(string fromText, string to, string from, DiploHubClientService hubClient, DictionaryTranslationResponse response)
        {
            var result = await translationService.TranslateAsync(fromText, to, from: from);

            if (result.IsSuccess)
            {
                response.TranslationCount++;
                return result.Text;
            }
            else
            {
                hubClient.SendAlert(new Alert(AlertType.Error, result.Message));
                logger.LogError(result.Message);
                response.ErrorCount++;
            }

            return null;
        }
    }
}
