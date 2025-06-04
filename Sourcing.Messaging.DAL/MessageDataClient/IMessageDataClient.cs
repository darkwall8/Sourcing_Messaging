using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sourcing.Messaging.DAL.DTOs;

namespace Sourcing.Messaging.DAL.MessageDataClient
{
    public interface IMessageDataClient
    {
        Task<IEnumerable<MessageDto>> GetMessagesAsync(string sender, string receiver);
        Task<bool> SendMessageAsync(MessageDto message);
        Task<IEnumerable<MessageDto>> GetAllMessagesAsync();
        Task MarkMessagesAsRead(string readerId, string otherUserId);

    }
}
