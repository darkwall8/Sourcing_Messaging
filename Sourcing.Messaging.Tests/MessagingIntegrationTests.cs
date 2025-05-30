using System.Net;
using System.Net.Http.Json;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Sourcing.Messaging.DAL.DTOs;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Sourcing.Messaging.API;
using Sourcing.Messaging.DAL.MessageDataClient;
using Microsoft.AspNetCore.SignalR.Client;

public class MessagingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;


    public MessagingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SendMessage_Returns200AndPersistsMessage()
    {
        // Arrange
        var message = new MessageDto
        {
            SenderId = "user1",
            ReceiverId = "user2",
            Content = "Bonjour entreprise Y"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/messages", message);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicResult<MessageDto>>();
        Assert.Equal(200, result.statusCode);
        Assert.Equal("Message envoyé avec succès.", result.message);
        Assert.Equal("user1", result.data.SenderId);

    }

    [Fact]
    public async Task GetConversation_ReturnsOrderedMessages()
    {
        FakeMessageDataClient.ClearMessages();


        // Arrange
        var msg1 = new MessageDto { SenderId = "user1", ReceiverId = "user2", Content = "Message 1" };
        var msg2 = new MessageDto { SenderId = "user2", ReceiverId = "user1", Content = "Message 2" };

        await _client.PostAsJsonAsync("/api/messages", msg1);
        await Task.Delay(20); // pour simuler une différence temporelle
        await _client.PostAsJsonAsync("/api/messages", msg2);

        // Act
        var response = await _client.GetAsync("/api/messages/user1/user2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicResult<List<MessageDto>>>();
        Assert.Equal(2, result.data.Count);
        Assert.True(result.data[0].SentAt < result.data[1].SentAt); // vérifie l'ordre
    }

    [Fact]
    public async Task GetUserConversations_ReturnsCorrectInbox()
    {
        FakeMessageDataClient.ClearMessages();

        // Arrange
        var user1 = "user1";
        var user2 = "user2";
        var user3 = "user3";

        // On envoie des messages dans les deux sens
        await _client.PostAsJsonAsync("/api/messages", new MessageDto { SenderId = user2, ReceiverId = user1, Content = "Msg from user2" });
        await _client.PostAsJsonAsync("/api/messages", new MessageDto { SenderId = user3, ReceiverId = user1, Content = "Msg from user3" });
        await _client.PostAsJsonAsync("/api/messages", new MessageDto { SenderId = user1, ReceiverId = user2, Content = "Reply to user2" });

        // Act
        var response = await _client.GetAsync($"/api/inbox/{user1}");

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DynamicResult<List<ConversationDto>>>();

        Assert.Equal(200, result.statusCode);
        Assert.Equal(2, result.data.Count); // user2 et user3

        var convWithUser2 = result.data.FirstOrDefault(c => c.OtherUserId == "user2");
        Assert.NotNull(convWithUser2);
        Assert.Equal("Reply to user2", convWithUser2.LastMessageContent);
        Assert.False(string.IsNullOrWhiteSpace(convWithUser2.LastMessageTime.ToString()));
    }

    [Fact]
    public async Task FullConversation_WithSignalR_ReceivesMessagesAndGetsHistory()
    {
        // 🧼 Nettoyage mémoire
        FakeMessageDataClient.ClearMessages();

        // 👤 Utilisateurs
        string sender = "user1";
        string receiver = "user2";

        // 🧠 Buffer pour stocker les messages reçus via SignalR
        List<string> realtimeReceived = new();

        // ⚡ Connexion SignalR simulée (user2)
        var baseUri = _client.BaseAddress!.ToString().TrimEnd('/');
        var hubUrl = $"{baseUri}/chat?userId={receiver}";



        var connection = new HubConnectionBuilder()
             .WithUrl(hubUrl, options =>
             {
                 options.HttpMessageHandlerFactory = _ => new HttpClientHandler
                 {
                     ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                 };
             })
             .Build();



        connection.On<SignalRMessage>("ReceiveMessage", msg =>
        {
            realtimeReceived.Add(msg.Content);
        });

        await connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connection.State);

        // 📤 Envoie 5 messages via l’API (REST)
        for (int i = 1; i <= 5; i++)
        {
            var dto = new MessageDto
            {
                SenderId = sender,
                ReceiverId = receiver,
                Content = $"Message {i}"
            };

            var response = await _client.PostAsJsonAsync("/api/messages", dto);
            response.EnsureSuccessStatusCode();
        }

        // ⏱️ Attendre un peu pour laisser le temps à SignalR de traiter
        await Task.Delay(500);

        // ✅ Vérifie que 5 messages ont bien été reçus en live
        Assert.Equal(5, realtimeReceived.Count);

        // 📥 Récupère toute la conversation via REST
        var historyResponse = await _client.GetAsync($"/api/messages/{sender}/{receiver}");
        historyResponse.EnsureSuccessStatusCode();

        var result = await historyResponse.Content.ReadFromJsonAsync<DynamicResult<List<MessageDto>>>();

        Assert.Equal(5, result.data.Count);
        Assert.Equal("Message 1", result.data.First().Content);
        Assert.Equal("Message 5", result.data.Last().Content);

        await connection.StopAsync();
    }





    // Classe pour lire dynamiquement les résultats JSON
    private class DynamicResult<T>
    {
        public int statusCode { get; set; }
        public string message { get; set; }
        public T data { get; set; }
    }

    private class ConversationDto
    {
        public string OtherUserId { get; set; }
        public string LastMessageContent { get; set; }
        public DateTime LastMessageTime { get; set; }
        public bool HasUnreadMessages { get; set; }
    }

    private class SignalRMessage
    {
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }

}
