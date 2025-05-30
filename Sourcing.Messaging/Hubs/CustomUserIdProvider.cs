using Microsoft.AspNetCore.SignalR;

namespace Sourcing.Messaging.API.Hubs
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // 👇 Permet d'utiliser la query string ?userId=userX
            return connection.GetHttpContext()?.Request.Query["userId"];
        }
    }
}
