using Sourcing.Messaging.DAL.DTOs;

namespace Sourcing.Messaging.BLL.Realtime
{
    public interface IRealtimeMessenger
    {
        Task SendMessageToUserAsync(string userId, MessageDto message);
    }
}
