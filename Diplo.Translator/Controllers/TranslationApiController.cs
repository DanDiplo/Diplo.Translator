using Diplo.Translator.Models;
using Diplo.Translator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;
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
        private readonly IDictionaryTranslationService dictionaryTranslationService;
        private readonly IOptions<TranslationConfig> configOptions;

        public TranslationApiController(IDictionaryTranslationService dictionaryTranslationService, IOptions<TranslationConfig> options)
        {
            this.dictionaryTranslationService = dictionaryTranslationService ?? throw new ArgumentNullException(nameof(IDictionaryTranslationService));
            this.configOptions = options;
        }

        [HttpPost]
        /// <summary>
        /// API endpoint for translating all empty dictionary items from the default language
        /// </summary>
        /// <remarks>
        /// See /Umbraco/backoffice/DiploTranslator/TranslationApi/TranslateAll?clientId=xxxx&fromCulture=en&overwrite=false
        /// </remarks>
        public async Task<DictionaryTranslationResponse> TranslateAll(string clientId, string fromCulture = null, bool overwrite = false)
        {
            return await this.dictionaryTranslationService.TranslateDictionary(clientId, fromCulture, overwrite);
        }

        /// <summary>
        /// Gets all languages
        /// </summary>
        /// <remarks>
        /// See /Umbraco/backoffice/DiploTranslator/TranslationApi/GetLanguages
        /// </remarks>
        [HttpGet]
        public IEnumerable<ILanguage> GetLanguages()
        {
            return this.dictionaryTranslationService.GetAllLanguages();
        }

        /// <summary>
        /// API endpoint for checking the config is OK
        /// </summary>
        /// <remarks>
        /// See /Umbraco/backoffice/DiploTranslator/TranslationApi/CheckConfiguration
        /// </remarks>
        [HttpGet]
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

        public class ConfigurationResponse
        {
            public bool Ok { get; set; }

            public string Message { get; set; }
        }
    }
}
