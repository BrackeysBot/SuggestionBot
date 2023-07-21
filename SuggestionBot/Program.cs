using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SuggestionBot.Data;
using SuggestionBot.Services;
using X10D.Hosting.DependencyInjection;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("data/config.json", true, true);

builder.Logging.ClearProviders();
builder.Logging.AddNLog();

builder.Services.AddSingleton(new DiscordClient(new DiscordConfiguration
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
    LoggerFactory = new NLogLoggerFactory(),
    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers | DiscordIntents.GuildMessages
}));

builder.Services.AddHostedService<LoggingService>();

builder.Services.AddDbContextFactory<SuggestionContext>();
builder.Services.AddHostedService<DatabaseService>();
builder.Services.AddHostedSingleton<DiscordLogService>();
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddHostedSingleton<SuggestionService>();
builder.Services.AddHostedSingleton<UserBlockingService>();
builder.Services.AddHostedSingleton<BotService>();

IHost app = builder.Build();
await app.RunAsync();
