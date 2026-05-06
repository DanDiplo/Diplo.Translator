using Diplo.Translator.Services;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Diplo.Translator;

public sealed class TranslatorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<TranslatorConfig>(builder.Config.GetSection(TranslatorConfig.ConfigSectionName));
        builder.Services.AddScoped<ITranslationService, UmbracoAiTranslationService>();
        builder.Services.AddScoped<IDictionaryTranslationService, DictionaryTranslationService>();
        builder.Services.AddSingleton<ITranslationJobManager, TranslationJobManager>();
    }
}
