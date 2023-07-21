using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Humanizer;
using SuggestionBot.Configuration;
using SuggestionBot.Data;
using SuggestionBot.Interactivity;
using SuggestionBot.Services;

namespace SuggestionBot.Commands;

internal sealed class SuggestCommand : ApplicationCommandModule
{
    private readonly ConfigurationService _configurationService;
    private readonly SuggestionService _suggestionService;
    private readonly UserBlockingService _userBlockingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SuggestCommand" /> class.
    /// </summary>
    /// <param name="configurationService">The <see cref="ConfigurationService" />.</param>
    /// <param name="suggestionService">The <see cref="SuggestionService" />.</param>
    /// <param name="userBlockingService">The <see cref="UserBlockingService" />.</param>
    public SuggestCommand(ConfigurationService configurationService, SuggestionService suggestionService,
        UserBlockingService userBlockingService)
    {
        _configurationService = configurationService;
        _suggestionService = suggestionService;
        _userBlockingService = userBlockingService;
    }

    [SlashCommand("suggest", "Submit a new suggestion.")]
    [SlashRequireGuild]
    public async Task SuggestAsync(InteractionContext context)
    {
        if (_userBlockingService.IsUserBlocked(context.Guild, context.User))
        {
            var builder = new DiscordInteractionResponseBuilder();
            builder.WithContent("You are blocked from posting suggestions in this server.");
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder)
                .ConfigureAwait(false);
            return;
        }

        if (ValidateCooldown(context, out DateTimeOffset expiration))
        {
            TimeSpan remaining = expiration - DateTimeOffset.UtcNow;

            var builder = new DiscordInteractionResponseBuilder();
            builder.WithContent($"You are on cooldown. You can suggest again in {remaining.Humanize()}.");
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder)
                .ConfigureAwait(false);
            return;
        }

        var modal = new DiscordModalBuilder(context.Client);
        modal.WithTitle("New Suggestion");

        DiscordModalTextInput input = modal.AddInput("What is your suggestion", "e.g. The server should have [...]",
            inputStyle: TextInputStyle.Paragraph, maxLength: 4000);
        DiscordModalResponse response =
            await modal.Build().RespondToAsync(context.Interaction, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

        if (response != DiscordModalResponse.Success) return;

        var followUp = new DiscordFollowupMessageBuilder();
        if (string.IsNullOrWhiteSpace(input.Value))
        {
            followUp.WithContent("No content provided. Suggestion cancelled.");
            await context.FollowUpAsync(followUp).ConfigureAwait(false);
            return;
        }

        Suggestion suggestion = _suggestionService.CreateSuggestion(context.Member, input.Value);
        DiscordMessage? message = await _suggestionService.PostSuggestionAsync(suggestion).ConfigureAwait(false);
        if (message == null)
        {
            followUp.WithContent("Failed to post suggestion. If this issue persists, please contact ModMail.");
            await context.FollowUpAsync(followUp).ConfigureAwait(false);
            return;
        }

        _suggestionService.UpdateSuggestionMessage(suggestion, message);
        followUp.WithContent($"Your suggestion has been created and can be viewed here: {message.JumpLink}");
        await context.FollowUpAsync(followUp).ConfigureAwait(false);
    }

    private bool ValidateCooldown(InteractionContext context, out DateTimeOffset expiration)
    {
        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? configuration))
        {
            configuration = new GuildConfiguration();
        }

        if (configuration.Cooldown <= 0)
        {
            expiration = default;
            return false;
        }

        DateTimeOffset lastSuggestionTime = _suggestionService.GetLastSuggestionTime(context.Guild, context.User);
        expiration = lastSuggestionTime + TimeSpan.FromSeconds(configuration.Cooldown);
        return expiration > DateTimeOffset.UtcNow;
    }
}
