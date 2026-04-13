using Microsoft.AspNetCore.SignalR;
using NetSentinel.Models;

namespace NetSentinel.Hubs
{
    public class NetworkHub : Hub
    {
        public async Task SendAlert(Alert alert)
        {
            await Clients.All.SendAsync("ReceiveAlert", alert);
        }

        public async Task SendLog(string message)
        {
            await Clients.All.SendAsync("ReceiveLog", message);
        }

        public async Task SendTrafficUpdate(TrafficSummary summary)
        {
            await Clients.All.SendAsync("ReceiveTrafficUpdate", summary);
        }

        public async Task SendDeviceUpdate(Device device)
        {
            await Clients.All.SendAsync("ReceiveDeviceUpdate", device);
        }
    }
}
