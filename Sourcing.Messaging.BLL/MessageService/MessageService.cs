using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly ILogger _logger;  // Utilisation de ILogger de Serilog

        // Constructeur avec injection de dépendances pour le client de données et le service de messagerie en temps réel
        public MessageService(IMessageDataClient dataClient, IRealtimeMessenger realtime, ILogger logger)
        {
            _dataClient = dataClient ?? throw new ArgumentNullException(nameof(dataClient));
            _realtime = realtime ?? throw new ArgumentNullException(nameof(realtime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Envoyer un message entre deux utilisateurs
        public async Task<ServiceResult<MessageDto>> SendMessageAsync(MessageDto dto)
        {
            _logger.Information("Tentative d'envoi du message de {SenderId} vers {ReceiverId}", dto.SenderId, dto.ReceiverId);

            try
            {
                // Validation des données
                if (dto == null)
                {
                    _logger.Warning("Le message envoyé de {SenderId} à {ReceiverId} est nul", dto.SenderId, dto.ReceiverId);
                    return ServiceResult<MessageDto>.BadRequest("Le message ne peut pas être nul.");
                }

                if (string.IsNullOrWhiteSpace(dto.Content))
                {
                    _logger.Warning("Le contenu du message de {SenderId} à {ReceiverId} est vide", dto.SenderId, dto.ReceiverId);
                    return ServiceResult<MessageDto>.BadRequest("Le contenu du message est vide.");
                }

                if (dto.Content.Length > 1000)
                {
                    _logger.Warning("Le message de {SenderId} est trop long (plus de 1000 caractères)", dto.SenderId);
                    return ServiceResult<MessageDto>.BadRequest("Le message est trop long (maximum 1000 caractères).");
                }

                if (string.IsNullOrWhiteSpace(dto.SenderId) || string.IsNullOrWhiteSpace(dto.ReceiverId))
                {
                    _logger.Warning("Les identifiants de l'expéditeur ou du destinataire sont manquants");
                    return ServiceResult<MessageDto>.BadRequest("Les identifiants d'expéditeur et de destinataire sont requis.");
                }

                if (dto.SenderId == dto.ReceiverId)
                {
                    _logger.Warning("L'expéditeur et le destinataire sont identiques : {SenderId}", dto.SenderId);
                    return ServiceResult<MessageDto>.BadRequest("L'expéditeur et le destinataire doivent être différents.");
                }

                // Initialisation du message
                dto.SentAt = DateTime.UtcNow;
                dto.IsRead = false;

                // Enregistrement du message
                var saved = await _dataClient.SendMessageAsync(dto);
                if (!saved)
                {
                    _logger.Error("Erreur lors de la sauvegarde du message de {SenderId} à {ReceiverId}", dto.SenderId, dto.ReceiverId);
                    return ServiceResult<MessageDto>.ServerError("Erreur lors de la sauvegarde du message.");
                }

                // Diffusion via SignalR
                await _realtime.SendMessageToUserAsync(dto.ReceiverId, dto);

                _logger.Information("Message envoyé avec succès de {SenderId} à {ReceiverId}", dto.SenderId, dto.ReceiverId);
                return ServiceResult<MessageDto>.Ok(dto, "Message envoyé avec succès.");
            }
            catch (Exception ex)
            {
                // Capture des erreurs et log
                _logger.Error(ex, "Erreur lors de l'envoi du message de {SenderId} à {ReceiverId}", dto.SenderId, dto.ReceiverId);
                return ServiceResult<MessageDto>.ServerError("Une erreur est survenue.");
            }
        }

        // Récupérer la conversation entre deux utilisateurs
        public async Task<ServiceResult<IEnumerable<MessageDto>>> GetConversationAsync(string userA, string userB)
        {
            _logger.Information("Tentative de récupération de la conversation entre {UserA} et {UserB}", userA, userB);

            // Validation des utilisateurs
            if (string.IsNullOrWhiteSpace(userA) || string.IsNullOrWhiteSpace(userB))
            {
                _logger.Warning("Identifiants manquants : {UserA}, {UserB}", userA, userB);
                return ServiceResult<IEnumerable<MessageDto>>.BadRequest("Les identifiants utilisateur sont requis.");
            }

            try
            {
                // Marquer les messages comme lus
                await _dataClient.MarkMessagesAsRead(userA, userB);
                _logger.Information("Messages marqués comme lus pour {UserA} et {UserB}", userA, userB);

                // Récupérer la conversation
                var messages = await _dataClient.GetMessagesAsync(userA, userB);

                _logger.Information("Conversation récupérée avec succès pour {UserA} et {UserB}. Nombre de messages : {MessageCount}", userA, userB, messages.Count());

                return ServiceResult<IEnumerable<MessageDto>>.Ok(messages.OrderBy(m => m.SentAt), "Conversation récupérée.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erreur lors de la récupération de la conversation entre {UserA} et {UserB}", userA, userB);
                return ServiceResult<IEnumerable<MessageDto>>.ServerError("Erreur lors de la récupération des messages.");
            }
        }

        // Récupérer toutes les conversations d'un utilisateur
        public async Task<ServiceResult<IEnumerable<ConversationDto>>> GetUserConversationsAsync(string userId)
        {
            _logger.Information("Tentative de récupération des conversations pour l'utilisateur {UserId}", userId);

            // Vérification de l'identifiant utilisateur
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.Warning("Identifiant utilisateur manquant");
                return ServiceResult<IEnumerable<ConversationDto>>.BadRequest("L'identifiant utilisateur est requis.");
            }

            try
            {
                // Récupérer tous les messages de l'utilisateur
                var messages = await _dataClient.GetAllMessagesAsync();

                var conversations = messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Select(group =>
                    {
                        // Récupérer le dernier message de la conversation
                        var lastMsg = group.OrderByDescending(m => m.SentAt).First();

                        // Vérification si le dernier message est non lu
                        bool lastMessageIsUnread = lastMsg.ReceiverId == userId && !lastMsg.IsRead;

                        return new ConversationDto
                        {
                            OtherUserId = group.Key,
                            LastMessageContent = lastMsg.Content,
                            Sender = lastMsg.SenderId,
                            LastMessageTime = lastMsg.SentAt,
                            HasUnreadMessages = lastMessageIsUnread
                        };
                    })
                    .OrderByDescending(c => c.LastMessageTime) // Trier les conversations par dernier message
                    .ToList();

                _logger.Information("Conversations récupérées avec succès pour l'utilisateur {UserId}. Nombre de conversations : {ConversationCount}", userId, conversations.Count);

                return ServiceResult<IEnumerable<ConversationDto>>.Ok(conversations, "Conversations récupérées avec succès.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erreur lors de la récupération des conversations pour l'utilisateur {UserId}", userId);
                return ServiceResult<IEnumerable<ConversationDto>>.ServerError("Une erreur est survenue.");
            }
        }
    }
}
