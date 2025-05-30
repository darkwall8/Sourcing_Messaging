using Microsoft.AspNetCore.SignalR;

namespace Sourcing.Messaging.API.Hubs
{
    public class ChatHub : Hub
    {
        // Envoi du message à un utilisateur spécifique
        public async Task SendMessage(string senderId, string receiverId, string content)
        {
            var timestamp = DateTime.UtcNow;
            await Clients.User(receiverId).SendAsync("ReceiveMessage", new
            {
                SenderId = senderId,
                Content = content,
                SentAt = timestamp
            });
        }

        // Marque tous les messages comme lus dans une conversation
        public async Task MarkMessagesAsRead(string userId, string otherUserId)
        {
            // Logique pour marquer comme lus (effectuer cette mise à jour dans la base ou fake DAL)
            // Simule que tous les messages entre les deux utilisateurs sont maintenant lus

            // Diffuse l’événement aux deux utilisateurs pour indiquer que les messages ont été lus
            await Clients.User(userId).SendAsync("MessagesRead", otherUserId);  // Notifie user1
            await Clients.User(otherUserId).SendAsync("MessagesRead", userId);  // Notifie user2
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"✅ Connexion SignalR : {Context.UserIdentifier} / ConnId: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"🔌 Déconnexion : {Context.UserIdentifier} / ConnId: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }
    }
}
