using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Sourcing.Messaging.BLL.MessageService;
using Sourcing.Messaging.BLL.Models;
using Sourcing.Messaging.BLL.Realtime;
using Sourcing.Messaging.DAL.DTOs;
using Sourcing.Messaging.DAL.MessageDataClient;

namespace Sourcing.Messaging.BLL.MessageService
{
    public class MessageService : IMessageService
    {
        private readonly IMessageDataClient _dataClient;
        private readonly IRealtimeMessenger _realtime;


        public MessageService(IMessageDataClient dataClient, IRealtimeMessenger realtime)
        {
            _dataClient = dataClient;
            _realtime = realtime;
        }


        public async Task<ServiceResult<IEnumerable<MessageDto>>> GetConversationAsync(string userA, string userB)
        {
            if (string.IsNullOrWhiteSpace(userA) || string.IsNullOrWhiteSpace(userB))
                return ServiceResult<IEnumerable<MessageDto>>.BadRequest("Identifiants manquants.");

            if (_dataClient is not FakeMessageDataClient fakeClient)
                return ServiceResult<IEnumerable<MessageDto>>.ServerError("Marquage disponible uniquement en mode dev.");

           

            var messages = await fakeClient.GetMessagesAsync(userA, userB);
            await fakeClient.MarkMessagesAsRead(userA, userB);

            return ServiceResult<IEnumerable<MessageDto>>.Ok(messages.OrderBy(m => m.SentAt), "Conversation récupérée.");
        }





        public async Task<ServiceResult<MessageDto>> SendMessageAsync(MessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
                return ServiceResult<MessageDto>.BadRequest("Le contenu du message est vide.");

            if (dto.SenderId == dto.ReceiverId)
                return ServiceResult<MessageDto>.BadRequest("L'expéditeur et le destinataire sont identiques.");

            dto.SentAt = DateTime.UtcNow;

            var saved = await _dataClient.SendMessageAsync(dto);
            if (!saved)
                return ServiceResult<MessageDto>.ServerError("Erreur lors de la sauvegarde du message.");

            await _realtime.SendMessageToUserAsync(dto.ReceiverId, dto);

            return ServiceResult<MessageDto>.Ok(dto, "Message envoyé avec succès.");
        }

        public async Task<ServiceResult<IEnumerable<ConversationDto>>> GetUserConversationsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return ServiceResult<IEnumerable<ConversationDto>>.BadRequest("L'identifiant utilisateur est requis.");

            if (_dataClient is not FakeMessageDataClient fakeClient)
                return ServiceResult<IEnumerable<ConversationDto>>.ServerError("Seule l’implémentation Fake est supportée pour cette fonctionnalité.");

            var messages = await fakeClient.GetAllMessagesAsync();

            var conversations = messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(group =>
                {
                    var lastMsg = group.OrderByDescending(m => m.SentAt).First();

                    // Le statut de lecture est pertinent uniquement si le dernier message est **reçu** par l'utilisateur
                    bool lastMessageIsUnread = lastMsg.ReceiverId == userId && !lastMsg.IsRead;

                    return new ConversationDto
                    {
                        OtherUserId = group.Key,
                        LastMessageContent = lastMsg.Content,
                        LastMessageTime = lastMsg.SentAt,
                        HasUnreadMessages = lastMessageIsUnread
                    };
                })
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();

            return ServiceResult<IEnumerable<ConversationDto>>.Ok(conversations, "Conversations récupérées avec succès.");
        }




    }
}