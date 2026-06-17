using Microsoft.EntityFrameworkCore;
using BattleTanksAPI.Hubs;
using BattleTanksAPI.Services;
using StackExchange.Redis;
using BattleTanks_Backend.Infrastructure.Data;
using BattleTanks_Backend.Domain.Interfaces;
using BattleTanks_Backend.Infrastructure.Repositories;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Application.Services;
using BattleTanks_Backend.Infrastructure.Middleware;
using StackExchange.Profiling;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Battle Tanks API",
        Version = "v1",
        Description = "API REST para el juego Battle Tanks Multiplayer"
    });
});

// Registro de SignalR
builder.Services.AddSignalR();

// Registro de Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddSingleton<EventHistoryService>();
builder.Services.AddSingleton<RedisCacheService>();

builder.Services.AddDbContext<BattleTanksDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));

// ReadOnlyDbContext para réplicas de lectura (Actividad 3)
builder.Services.AddDbContext<ReadOnlyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReadOnlyConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));

builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
builder.Services.AddScoped<IScoreRepository, ScoreRepository>();
builder.Services.AddScoped<IRoomPlayerRepository, RoomPlayerRepository>();

builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IGameSessionService, GameSessionService>();
builder.Services.AddScoped<IScoreService, ScoreService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IChatService, ChatService>();

// Servicio de benchmarking para medir rendimiento de consultas
builder.Services.AddScoped<QueryBenchmarkService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddMiniProfiler(options =>
{
    options.RouteBasePath = "/profiler";
    options.ColorScheme = ColorScheme.Dark;
    options.PopupShowTrivial = true;
    options.PopupShowTimeWithChildren = true;
    options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();
}).AddEntityFramework();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Battle Tanks API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAngular");

app.UseMiniProfiler();

app.UseFirebaseAuth();

app.UseAuthorization();

app.MapControllers();

// Mapear hub de SignalR
app.MapHub<GameHub>("/game");


Console.WriteLine("BATTLE TANKS BACKEND - INICIADO");
Console.WriteLine("API REST:    http://localhost:5013");
Console.WriteLine("Swagger UI:  http://localhost:5013/swagger");
Console.WriteLine("SignalR Hub: ws://localhost:5013/game");
Console.WriteLine("Redis:       localhost:6379");

app.Run();
