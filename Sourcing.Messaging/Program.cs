using Sourcing.Messaging.DAL.MessageDataClient;
using Sourcing.Messaging.BLL.MessageService;
using Sourcing.Messaging.BLL.Realtime;
using Sourcing.Messaging.API.Services;
using Sourcing.Messaging.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// Configurer Serilog pour capturer uniquement les erreurs détaillées
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Error() // Enregistrer uniquement les erreurs
    .WriteTo.Console(new JsonFormatter()) // Format JSON compact pour la console
    .WriteTo.File(
        "logs/errors-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 1073741824
    )
    .CreateLogger();

// Utiliser Serilog pour les logs
builder.Host.UseSerilog();

// ➕ Injection de l'instance Serilog.ILogger dans le DI
builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);

// ➕ SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// ➕ Fake DAL 
builder.Services.AddScoped<IMessageDataClient, MessageDataClient>();

// ➕ MongoDbConnection
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDbConnection")));
builder.Services.AddScoped<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase("SourcingMessaging"));

// ➕ BLL
builder.Services.AddScoped<IMessageService, MessageService>();

// ➕ Service de diffusion temps réel
builder.Services.AddScoped<IRealtimeMessenger, SignalRMessenger>();

// ➕ Swagger & Controllers
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Utiliser Serilog pour la journalisation des requêtes HTTP
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsedMilliseconds, ex) =>
    {
        if (httpContext.Response.StatusCode >= 500)
            return Serilog.Events.LogEventLevel.Error;
        else if (httpContext.Response.StatusCode >= 400)
            return Serilog.Events.LogEventLevel.Warning;
        else
            return Serilog.Events.LogEventLevel.Information;
    };
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers(); // REST
app.MapHub<ChatHub>("/chat"); // SignalR WebSocket

app.Run();

// Nécessaire pour WebApplicationFactory<> en test
public partial class Program { }
