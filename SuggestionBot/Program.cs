using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using SuggestionBot.Data;
using SuggestionBot.Services;
using X10D.Hosting.DependencyInjection;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/latest.log", rollingInterval: RollingInterval.Day)
#if DEBUG
    .MinimumLevel.Debug()
#endif
    .CreateLogger();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("data/config.json", true, true);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddSingleton(new DiscordClient(new DiscordConfiguration
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
    LoggerFactory = new SerilogLoggerFactory(),
    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers | DiscordIntents.GuildMessages
}));

builder.Services.AddDbContextFactory<SuggestionContext>();
builder.Services.AddHostedService<DatabaseService>();
builder.Services.AddHostedSingleton<DiscordLogService>();
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddSingleton<CooldownService>();
builder.Services.AddHostedSingleton<SuggestionService>();
builder.Services.AddHostedSingleton<UserBlockingService>();
builder.Services.AddSingleton<MailmanService>();
builder.Services.AddHostedSingleton<BotService>();

IHost app = builder.Build();
await app.RunAsync();
