using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sourcing.Messaging.DAL.DTOs;

namespace Sourcing.Messaging.DAL.MessageDataClient
{
    public class FakeMessageDataClient : IMessageDataClient
    {
        private static readonly List<MessageDto> _messages = new();

        public Task<bool> SendMessageAsync(MessageDto message)
        {
            message.SentAt = DateTime.UtcNow;
            _messages.Add(message);
            return Task.FromResult(true);
        }

        public Task<IEnumerable<MessageDto>> GetMessagesAsync(string sender, string receiver)
        {
            var results = _messages
                .Where(m =>
                    (m.SenderId == sender && m.ReceiverId == receiver) ||
                    (m.SenderId == receiver && m.ReceiverId == sender))
                .OrderBy(m => m.SentAt)
                .ToList();

            return Task.FromResult<IEnumerable<MessageDto>>(results);
        }

        public Task<IEnumerable<MessageDto>> GetAllMessagesAsync()
        {
            return Task.FromResult<IEnumerable<MessageDto>>(_messages);
        }

        public Task MarkMessagesAsRead(string readerId, string otherUserId)
        {
            foreach (var msg in _messages)
            {
                if (msg.ReceiverId == readerId && msg.SenderId == otherUserId && !msg.IsRead)
                {
                    msg.IsRead = true;
                }
            }

            return Task.CompletedTask;
        }


        public static void ClearMessages()
        {
            _messages.Clear();
        }



    }
}
