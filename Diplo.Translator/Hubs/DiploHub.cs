using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Diplo.Translator.Hubs
{
    /// <summary>
    /// SignalR Hub
    /// </summary>
    /// <remarks>
    /// Blatantly stolen from Kevin - https://github.com/KevinJump/uSync/blob/v10/dev/uSync.BackOffice/Hubs/SyncHub.cs
    /// </remarks>
    public class DiploHub : Hub<ISyncHub>
    {
        /// <summary>
        ///  Get the current time 
        /// </summary>
        /// <remarks>
        /// Used to give the hub a porpoise - not called 
        /// </remarks>
        public string GetTime() => DateTime.Now.ToString();
    }

    /// <summary>
    ///  Interface for the ISyncHub
    /// </summary>
    public interface ISyncHub
    {
        /// <summary>
        /// Refreshes the hub (other beers cannot reach)
        /// </summary>
        Task refreshed(int id);
    }
}

