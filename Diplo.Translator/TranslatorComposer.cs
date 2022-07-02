using Diplo.GodMode.Menus;
using Diplo.Translator.Hubs;
using Diplo.Translator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Diplo.Translator
{
    /// <summary>
    /// Used for DI
    /// </summary>
    public class TranslatorComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Config

            builder.Services.Configure<TranslationConfig>(builder.Config.GetSection(TranslationConfig.ConfigSectionName));

            builder.AddNotificationHandler<ServerVariablesParsingNotification, DiploServerVariablesHandler>();

            // Menu

            builder.AddNotificationHandler<MenuRenderingNotification, DictionaryNotificationHandler>();

            // Translator API

            builder.Services.AddScoped<IHttpJsonService, HttpJsonService>();
            builder.Services.AddScoped<ITranslationService, MicrosoftTranslatorService>();

            // SignalR Hubs

            builder.Services.AddSingleton<DiploHubRoutes>();
            builder.Services.AddSignalR();
            builder.Services.AddDiploSignalR();
        }
    }

    internal static class Extensions
    {
        public static IServiceCollection AddDiploSignalR(this IServiceCollection services)
        {
            services.Configure<UmbracoPipelineOptions>(options =>
            {
                options.AddFilter(new UmbracoPipelineFilter(
                    "diploTranslator",
                    applicationBuilder => { },
                    applicationBuilder => { },
                    applicationBuilder =>
                    {
                        applicationBuilder.UseEndpoints(e =>
                        {
                            var hubRoutes = applicationBuilder.ApplicationServices.GetRequiredService<DiploHubRoutes>();
                            hubRoutes.CreateRoutes(e);
                        });
                    }
                ));
            });

            return services;
        }
    }
}
