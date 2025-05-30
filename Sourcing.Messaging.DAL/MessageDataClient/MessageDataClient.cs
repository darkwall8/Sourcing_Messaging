using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sourcing.Messaging.DAL.DTOs;

namespace Sourcing.Messaging.DAL.MessageDataClient
{
    public class MessageDataClient : IMessageDataClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MessageDataClient> _logger;

        public MessageDataClient(HttpClient httpClient, ILogger<MessageDataClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(string sender, string receiver)
        {
            var response = await _httpClient.GetAsync($"/api/messages/{sender}/{receiver}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Erreur lors de la récupération des messages : {StatusCode}", response.StatusCode);
                return Enumerable.Empty<MessageDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IEnumerable<MessageDto>>(content);
        }

        public async Task<bool> SendMessageAsync(MessageDto message)
        {
            var json = JsonConvert.SerializeObject(message);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/messages", httpContent);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Erreur lors de l'envoi du message : {StatusCode}", response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }


    }
}
