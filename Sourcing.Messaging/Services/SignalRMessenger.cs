using Microsoft.AspNetCore.SignalR;
using Sourcing.Messaging.BLL.Realtime;
using Sourcing.Messaging.DAL.DTOs;
using Sourcing.Messaging.API.Hubs;

namespace Sourcing.Messaging.API.Services
{
    public class SignalRMessenger : IRealtimeMessenger
    {
        private readonly IHubContext<ChatHub> _hub;

        public SignalRMessenger(IHubContext<ChatHub> hub)
        {
            _hub = hub;
        }

        public Task SendMessageToUserAsync(string userId, MessageDto message)
        {
            return _hub.Clients.User(userId).SendAsync("ReceiveMessage", new
            {
                senderId = message.SenderId,
                content = message.Content,
                sentAt = message.SentAt
            });
        }
    }
}
