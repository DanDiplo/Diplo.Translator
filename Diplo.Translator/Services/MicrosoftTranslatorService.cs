using Diplo.Translator.Models;
using Diplo.Translator.Models.Microsoft;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Diplo.Translator.Services
{
    /// <summary>
    /// Translation service using Microsoft Cognitive Translator
    /// </summary>
    public class MicrosoftTranslatorService : ITranslationService
    {
        private readonly TranslationConfig config;
        private readonly IHttpJsonService httpJsonService;

        public MicrosoftTranslatorService(IOptions<TranslationConfig> options, IHttpJsonService httpJsonService)
        {
            this.httpJsonService = httpJsonService ?? throw new ArgumentNullException(nameof(httpJsonService));
            this.config = options.Value;
        }

        public async Task<TranslationResponse> TranslateAsync(string text, string to, string from = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (string.IsNullOrEmpty(to))
            {
                throw new ArgumentNullException(to);
            }

            string endpoint = Path.Combine(this.config.TranslatorApiEndpoint, "translate");

            var qs = new Dictionary<string, string>()
            {
                { "api-version", this.config.TranslatorApiVersion },
                { "to", to }
            };

            if (!string.IsNullOrEmpty(from))
            {
                qs.Add("from", from);
            }

            var headers = new Dictionary<string, string>()
            {
                { "Ocp-Apim-Subscription-Key", config.TranslatorApiKey }
            };

            var translationRequest = new TranslationRequest[] { new TranslationRequest(text) };

            var result = await httpJsonService.Post<IEnumerable<TranslationRequest>, IEnumerable<TranslationResult>>(endpoint, translationRequest, qs, headers);

            var response = new TranslationResponse()
            {
                IsSuccess = result.IsSuccess,
                Message = result.Message,
                To = to
            };

            if (result.IsSuccess)
            {
                var data = result.Model.First();
                var translation = data.Translations.First();
                response.Text = translation.Text;
                response.From = from ?? data.DetectedLanguage?.Language;
            }

            return response;
        }
    }
}
