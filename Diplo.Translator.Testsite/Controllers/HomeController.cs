using Diplo.Translator.Models.Microsoft;
using Diplo.Translator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Diplo.Translator.Testsite.Controllers
{
    // Example render controller injecting translation config via Options pattern

    public class HomeController : RenderController
    {
        private readonly TranslationConfig translationConfig;
        private readonly IHttpJsonService httpJsonService;

        public HomeController(ILogger<HomeController> logger, ICompositeViewEngine compositeViewEngine, IUmbracoContextAccessor umbracoContextAccessor, IOptions<TranslationConfig> options, IHttpJsonService httpJsonService)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            this.translationConfig = options.Value;
            this.httpJsonService = httpJsonService;
        }

        public override IActionResult Index()
        {
            string endpoint = this.translationConfig.TranslatorApiEndpoint + "translate";

            var qs = new Dictionary<string, string>()
            {
                { "api-version", this.translationConfig.TranslatorApiVersion },
                { "to", "fr" }
            };

            var headers = new Dictionary<string, string>()
            {
                { "Ocp-Apim-Subscription-Key", translationConfig.TranslatorApiKey }
            };

            var translationRequest = new TranslationRequest[] { new TranslationRequest("Hello world") };

            var result = Task.Run(() => httpJsonService.Post<IEnumerable<TranslationRequest>, IEnumerable<TranslationResult>>(endpoint, translationRequest, qs, headers)).Result;

            return CurrentTemplate(CurrentPage);
        }
    }
}
