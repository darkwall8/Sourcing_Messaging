using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Sourcing.Messaging.DAL.DTOs;

namespace Sourcing.Messaging.DAL.MessageDataClient
{
    public class MessageDataClient : IMessageDataClient
    {
        private readonly IMongoCollection<MessageDto> _messagesCollection;

        public MessageDataClient(IMongoDatabase database)
        {
            _messagesCollection = database.GetCollection<MessageDto>("Messages");
        }

        public async Task<bool> SendMessageAsync(MessageDto message)
        {
            await _messagesCollection.InsertOneAsync(message);
            return true;
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(string senderId, string receiverId)
        {
            var filter = Builders<MessageDto>.Filter.Where(m =>
                (m.SenderId == senderId && m.ReceiverId == receiverId) ||
                (m.SenderId == receiverId && m.ReceiverId == senderId));

            var messages = await _messagesCollection.Find(filter)
                .SortBy(m => m.SentAt)
                .ToListAsync();

            return messages;
        }

        public async Task<IEnumerable<MessageDto>> GetAllMessagesAsync()
        {
            var allMessages = await _messagesCollection.Find(_ => true).ToListAsync();
            return allMessages;
        }

        public async Task MarkMessagesAsRead(string readerId, string otherUserId)
        {
            var filter = Builders<MessageDto>.Filter.And(
                Builders<MessageDto>.Filter.Eq(m => m.ReceiverId, readerId),
                Builders<MessageDto>.Filter.Eq(m => m.SenderId, otherUserId),
                Builders<MessageDto>.Filter.Eq(m => m.IsRead, false)
            );

            var update = Builders<MessageDto>.Update.Set(m => m.IsRead, true);

            await _messagesCollection.UpdateManyAsync(filter, update);
        }

        //public static void ClearMessages()
        //{
        //    _messagesCollection.Clear();
        //}
    }
}
