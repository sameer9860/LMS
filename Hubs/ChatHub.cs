using Microsoft.AspNetCore.SignalR;

namespace LMS.Hubs
{
    public class ChatHub : Hub
    {
        // Broadcast message to all clients
        public async Task SendMessage(string user, string message, string time)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message, time);
        }
    }
}
