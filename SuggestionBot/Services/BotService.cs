using System.Reflection;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuggestionBot.Commands;

namespace SuggestionBot.Services;

/// <summary>
///     Represents a service which manages the bot's Discord connection.
/// </summary>
internal sealed class BotService : BackgroundService
{
    private readonly ILogger<BotService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordClient _discordClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BotService" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="discordClient">The Discord client.</param>
    public BotService(ILogger<BotService> logger, IServiceProvider serviceProvider, DiscordClient discordClient)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _discordClient = discordClient;

        var attribute = typeof(BotService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Version = attribute?.InformationalVersion ?? "Unknown";
    }

    /// <summary>
    ///     Gets the date and time at which the bot was started.
    /// </summary>
    /// <value>The start timestamp.</value>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>
    ///     Gets the bot version.
    /// </summary>
    /// <value>The bot version.</value>
    public string Version { get; }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _discordClient.DisconnectAsync().ConfigureAwait(false);
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("SuggestionBot v{Version} is starting...", Version);

        SlashCommandsExtension? slashCommands = _discordClient.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = _serviceProvider
        });

        slashCommands.RegisterCommands<InfoCommand>();
        slashCommands.RegisterCommands<SuggestCommand>();
        slashCommands.RegisterCommands<SuggestionCommand>();

        _discordClient.Ready += OnReady;
        return _discordClient.ConnectAsync();
    }

    private Task OnReady(DiscordClient sender, ReadyEventArgs args)
    {
        var activity = new DiscordActivity("/suggest to make suggestions", ActivityType.Playing);
        return _discordClient.UpdateStatusAsync(activity);
    }
}
