using Diplo.Translator.Hubs;
using System.Collections.Generic;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Diplo.Translator
{
    /// <summary>
    /// Adds variables that are accessible in Anguler
    /// </summary>
    /// <remarks>
    /// See https://24days.in/umbraco-cms/2021/umbraco-9-server-variables/
    /// </remarks>
    public class DiploServerVariablesHandler : INotificationHandler<ServerVariablesParsingNotification>
    {
        private readonly DiploHubRoutes diploHubRoutes;

        public DiploServerVariablesHandler(DiploHubRoutes hubRoutes)
        {
            this.diploHubRoutes = hubRoutes;
        }

        public void Handle(ServerVariablesParsingNotification notification)
        {
            notification.ServerVariables.Add("diploTranslator", new Dictionary<string, object>
            {
                { "signalRHub",  diploHubRoutes.GetDiploHubRoute() }
            });
        }
    }
}
