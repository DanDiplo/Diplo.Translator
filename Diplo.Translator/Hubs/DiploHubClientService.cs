using Diplo.Translator.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Diplo.Translator.Hubs
{
    /// <summary>
    /// Used for sending server side messages to client
    /// </summary>
    public class DiploHubClientService
    {
        private readonly IHubContext<DiploHub> hubContext;
        private readonly string clientId;

        /// <summary>
        /// Construct a new SignlaR hub client with the specified client identifier
        /// </summary>
        public DiploHubClientService(IHubContext<DiploHub> hubContext, string clientId)
        {
            this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            this.clientId = clientId;
        }

        /// <summary>
        /// Send an <paramref name="item"/> to the client with the message name of <paramref name="name"/>
        /// </summary>
        /// <param name="name">The name of the message, referenced in the client-side code</param>
        /// <param name="item">An object serialised to JSON that is sent as the message</param>
        public void SendMessage<TObject>(string name, TObject item)
        {
            if (hubContext != null && !string.IsNullOrWhiteSpace(clientId))
            {
                var client = hubContext.Clients.Client(clientId);
                if (client != null)
                {
                    client.SendAsync(name, item).Wait();
                    return;
                }

                hubContext.Clients.All.SendAsync(name, item).Wait();
            }
        }

        /// <summary>
        /// Send an <paramref name="item"/> to the client with the message name of <paramref name="name"/>
        /// </summary>
        /// <param name="name">The name of the message, referenced in the client-side code</param>
        /// <param name="item">An object serialised to JSON that is sent as the message</param>
        public async Task SendMessageAsync<TObject>(string name, TObject item)
        {
            if (hubContext != null && !string.IsNullOrWhiteSpace(clientId))
            {
                var client = hubContext.Clients.Client(clientId);
                if (client != null)
                {
                    await client.SendAsync(name, item);
                    return;
                }

                await hubContext.Clients.All.SendAsync(name, item);
            }
        }

        public void SendAlert(Alert alert) => SendMessage("alert", alert);

        public async Task SendAlertAsync(Alert alert) => await SendMessageAsync("alert", alert);
    }
}
