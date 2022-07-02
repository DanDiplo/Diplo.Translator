using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Routing;
using Umbraco.Extensions;

namespace Diplo.Translator.Hubs
{
    /// <summary>
    /// Used for routing signalR hub
    /// </summary>
    public class DiploHubRoutes : IAreaRoutes
    {
        private readonly IRuntimeState runtimeState;
        private readonly string umbracoPathSegment;

        /// <summary>
        ///  Constructor (called via DI)
        /// </summary>
        public DiploHubRoutes(IOptions<GlobalSettings> globalSettings, IHostingEnvironment hostingEnvironment, IRuntimeState runtimeState)
        {
            this.runtimeState = runtimeState;
            this.umbracoPathSegment = globalSettings.Value.GetUmbracoMvcArea(hostingEnvironment);
        }

        /// <summary>
        /// Create the signalR routes for the Hub
        /// </summary>
        public void CreateRoutes(IEndpointRouteBuilder endpoints)
        {
            switch (this.runtimeState.Level)
            {
                case Umbraco.Cms.Core.RuntimeLevel.Run:
                    endpoints.MapHub<DiploHub>(GetDiploHubRoute());
                    break;
            }
        }

        /// <summary>
        /// Get the path to the hub SignalR route
        /// </summary>
        public string GetDiploHubRoute()
        {
            return $"/{this.umbracoPathSegment}/{nameof(DiploHub)}";
        }
    }
}
