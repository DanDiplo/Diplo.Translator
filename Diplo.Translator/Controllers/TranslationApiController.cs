using Diplo.Translator.Hubs;
using Diplo.Translator.Models;
using Diplo.Translator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IOptions<TranslationConfig> configOptions;

        public TranslationApiController(ILocalizationService localizationService, ITranslationService translationService, ILogger<TranslationApiController> logger, IHubContext<DiploHub> hubContext, IOptions<TranslationConfig> options)
        {
            this.localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            this.translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            this.configOptions = options;
        }

        [HttpPost]
        /// <summary>
        /// API endpoint for translating all empty dictionary items from the default language
        /// </summary>
        /// <remarks>
        /// See /Umbraco/backoffice/DiploTranslator/TranslationApi/TranslateAll
        /// </remarks>
        public async Task<TranslationResponse> TranslateAll(string clientId)
        {
            var hubClient = new DiploHubClientService(hubContext, clientId);

            await hubClient.SendAlertAsync(new Alert("Translating..."));

            var languages = this.localizationService.GetAllLanguages().ToList();
            var defaultLanguage = languages.First(x => x.IsDefault);

            var response = new TranslationResponse();

            var rootDictionary = this.localizationService.GetRootDictionaryItems();

            var dictionaryItems = rootDictionary.Concat(rootDictionary.SelectMany(r => localizationService.GetDictionaryItemDescendants(r.Key)));

            response.DictionaryCount = dictionaryItems.Count();

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
                            string translatedText = await this.TranslateDictionaryItem(defaultTranslation.Value, lang.CultureInfo.Name, defaultLanguage.CultureInfo.Name, hubClient, response);

                            if (!string.IsNullOrEmpty(translatedText))
                            {
                                try
                                {
                                    this.localizationService.AddOrUpdateDictionaryValue(entry, lang, translatedText);
                                    this.localizationService.Save(entry);
                                    await hubClient.SendAlertAsync(new Alert(AlertType.Success, $"{lang.CultureInfo.Name}: {defaultTranslation.Value}' => '{translatedText}"));
                                }
                                catch (Exception ex)
                                {
                                    var message = $"Error translating {lang.CultureInfo.Name}: {defaultTranslation.Value}";
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

        private async Task<string> TranslateDictionaryItem(string fromText, string to, string from, DiploHubClientService hubClient, TranslationResponse response)
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

        /// <summary>
        /// API endpoint for checking the config is OK
        /// </summary>
        /// <remarks>
        /// See /Umbraco/backoffice/DiploTranslator/TranslationApi/CheckConfiguration
        /// </remarks>
        public ConfigurationResponse CheckConfiguration()
        {
            if (configOptions == null || configOptions.Value == null || !configOptions.Value.IsConfigured())
            {
                return new ConfigurationResponse()
                {  
                    Ok = false,
                    Message = "You need to set the TranslatorApiEndpoint and TranslatorApiKey in your configuration settings before you can use this. Please consult the documentation."
                };
            }

            return new ConfigurationResponse()
            {
                Ok = true
            };
        }

        /// <summary>
        /// Used to return a response to client
        /// </summary>
        public class TranslationResponse
        {
            /// <summary>
            /// How many items in dictionary
            /// </summary>
            public int DictionaryCount { get; set; }

            /// <summary>
            /// How many items translated
            /// </summary>
            public int TranslationCount { get; set; }

            /// <summary>
            /// How many items errored
            /// </summary>
            public int ErrorCount { get; set; }

            /// <summary>
            /// A message
            /// </summary>
            public string Message => $"Translated {TranslationCount} out of {DictionaryCount} items with {ErrorCount} errors";
        }

        public class ConfigurationResponse
        {
            public bool Ok { get; set; }

            public string Message { get; set; }
        }
    }
}
