using System.Net;
using System.Net.Http.Json;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Sourcing.Messaging.DAL.DTOs;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Client;
using System.Linq;
using MongoDB.Driver;
using Sourcing.Messaging.API;

public class MessagingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly IMongoCollection<MessageDto> _messagesCollection;

    public MessagingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();

        // Initialiser la connexion MongoDB pour nettoyage des collections avant tests
        var mongoClient = new MongoClient("mongodb+srv://etienne:azerty1234@cluster0.oll054k.mongodb.net/SourcingMessaging?retryWrites=true&w=majority");
        var database = mongoClient.GetDatabase("SourcingMessaging");
        _messagesCollection = database.GetCollection<MessageDto>("Messages");
    }

    // Nettoyer la collection Messages avant chaque test
    private async Task ClearMessagesAsync()
    {
        await _messagesCollection.DeleteManyAsync(FilterDefinition<MessageDto>.Empty);
    }

    [Fact]
    public async Task SendMessage_Returns200AndPersistsMessage()
    {
        await ClearMessagesAsync();

        var message = new MessageDto
        {
            SenderId = "user1",
            ReceiverId = "user2",
            Content = "Bonjour entreprise Y"
        };

        var response = await _client.PostAsJsonAsync("/api/messages", message);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicResult<MessageDto>>();
        Assert.Equal(200, result.statusCode);
        Assert.Equal("Message envoyé avec succès.", result.message);
        Assert.Equal("user1", result.data.SenderId);

        // Vérifier que le message a bien été inséré dans la base MongoDB
        var insertedMessage = await _messagesCollection.Find(m => m.Content == "Bonjour entreprise Y").FirstOrDefaultAsync();
        Assert.NotNull(insertedMessage);
        Assert.Equal("user1", insertedMessage.SenderId);
        Assert.Equal("user2", insertedMessage.ReceiverId);
    }

    [Fact]
    public async Task GetConversation_ReturnsOrderedMessages()
    {
        await ClearMessagesAsync();

        // Arrange
        var msg1 = new MessageDto { SenderId = "user1", ReceiverId = "user2", Content = "Message 1" };
        var msg2 = new MessageDto { SenderId = "user2", ReceiverId = "user1", Content = "Message 2" };

        await _client.PostAsJsonAsync("/api/messages", msg1);
        await Task.Delay(20); // Pour simuler une différence temporelle
        await _client.PostAsJsonAsync("/api/messages", msg2);

        // Act
        var response = await _client.GetAsync("/api/messages/user1/user2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicResult<List<MessageDto>>>();
        Assert.Equal(2, result.data.Count);
        Assert.True(result.data[0].SentAt < result.data[1].SentAt); // Vérifie l'ordre des messages
    }

    [Fact]
    public async Task GetUserConversations_ReturnsCorrectInbox()
    {
        await ClearMessagesAsync();

        var user1 = "user1";
        var user2 = "user2";
        var user3 = "user3";

        // Envoie des messages dans les deux sens
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
        await ClearMessagesAsync();

        string sender = "user1";
        string receiver = "user2";

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

        List<string> realtimeReceived = new();

        connection.On<SignalRMessage>("ReceiveMessage", msg =>
        {
            realtimeReceived.Add(msg.Content);
        });

        await connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connection.State);

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

        await Task.Delay(500);

        Assert.Equal(5, realtimeReceived.Count);

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
