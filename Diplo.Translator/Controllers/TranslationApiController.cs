using Diplo.Translator.Hubs;
using Diplo.Translator.Models;
using Diplo.Translator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;

namespace Diplo.Translator.Controllers
{
    /// <summary>
    /// Backend API controller that handles requests from the client
    /// </summary>
    [PluginController("DiploTranslator")]
    public class TranslationApiController : UmbracoAuthorizedJsonController
    {
        private readonly ITranslationService translationService;
        private readonly ILocalizationService localizationService;
        private readonly ILogger<TranslationApiController> logger;
        private readonly IHubContext<DiploHub> hubContext;

        public TranslationApiController(ILocalizationService localizationService, ITranslationService translationService, ILogger<TranslationApiController> logger, IHubContext<DiploHub> hubContext)
        {
            this.localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            this.translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        [HttpPost]
        /// <summary>
        /// API endpoint for translating all empty dictionary items from the default language
        /// </summary>
        /// <remarks>
        /// See /Umbraco/backoffice/DiploTranslator/TranslationApi/TranslateAll
        /// </remarks>
        public async Task<int> TranslateAll(string clientId)
        {
            var hubClient = new DiploHubClientService(hubContext, clientId);

            var languages = this.localizationService.GetAllLanguages().ToList();
            var defaultLanguage = languages.First(x => x.IsDefault);

            int translatedCount = 0;

            var rootDictionary = this.localizationService.GetRootDictionaryItems();

            var dictionaryItems = rootDictionary.Concat(rootDictionary.SelectMany(r => localizationService.GetDictionaryItemDescendants(r.Key)));

            await hubClient.SendAlertAsync(new Alert($"Found {dictionaryItems.Count()} dictionary items"));

            foreach (var entry in dictionaryItems)
            {
                foreach (var lang in languages)
                {
                    var translation = entry.Translations.FirstOrDefault(x => x.LanguageId == lang.Id);
                    var defaultTranslation = entry.Translations.FirstOrDefault(x => x.LanguageId == defaultLanguage.Id);

                    if (defaultTranslation != null && !string.IsNullOrEmpty(defaultTranslation.Value))
                    {
                        if (translation == null || string.IsNullOrEmpty(translation.Value))
                        {
                            string translatedText = await this.TranslateDictionaryItem(defaultTranslation.Value, lang.CultureInfo.Name, defaultLanguage.CultureInfo.Name, hubClient);

                            if (!string.IsNullOrEmpty(translatedText))
                            {
                                try
                                {
                                    this.localizationService.AddOrUpdateDictionaryValue(entry, lang, translatedText);
                                    this.localizationService.Save(entry);
                                    await hubClient.SendAlertAsync(new Alert(AlertType.Success, $"{lang.CultureInfo.Name}: {defaultTranslation.Value}' => '{translatedText}"));
                                    translatedCount++;
                                }
                                catch (Exception ex)
                                {
                                    var message = $"Error translating {lang.CultureInfo.Name}: {defaultTranslation.Value}";
                                    logger.LogError(message, ex);
                                    await hubClient.SendAlertAsync(new Alert(AlertType.Error, message));
                                }
                            }
                        }
                    }
                }
            }

            await hubClient.SendAlertAsync(new Alert(AlertType.Success, "Finished translating..."));

            return translatedCount;
        }

        private async Task<string> TranslateDictionaryItem(string fromText, string to, string from, DiploHubClientService hubClient)
        {
            var response = await translationService.TranslateAsync(fromText, to, from: from);

            if (response.IsSuccess)
            {
                return response.Text;
            }
            else
            {
                hubClient.SendAlert(new Alert(AlertType.Error, response.Message));
                logger.LogError(response.Message);
            }

            return null;
        }
    }
}
