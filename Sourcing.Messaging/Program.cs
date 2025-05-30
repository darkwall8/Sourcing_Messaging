using Sourcing.Messaging.DAL.MessageDataClient;
using Sourcing.Messaging.BLL.MessageService;
using Sourcing.Messaging.BLL.Realtime;
using Sourcing.Messaging.API.Services;
using Sourcing.Messaging.API.Hubs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// ➕ SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// ➕ Fake DAL (simule IMM)
builder.Services.AddSingleton<IMessageDataClient, FakeMessageDataClient>();

// ➕ BLL
builder.Services.AddScoped<IMessageService, MessageService>();

// ➕ Service de diffusion temps réel
builder.Services.AddScoped<IRealtimeMessenger, SignalRMessenger>();

// ➕ Swagger & Controllers
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();                         // REST
app.MapHub<ChatHub>("/chat");                 // SignalR WebSocket

app.Run();

// Nécessaire pour WebApplicationFactory<> en test
public partial class Program { }
