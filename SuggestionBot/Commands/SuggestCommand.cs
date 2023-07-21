using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Humanizer;
using SuggestionBot.Data;
using SuggestionBot.Interactivity;
using SuggestionBot.Services;

namespace SuggestionBot.Commands;

internal sealed class SuggestCommand : ApplicationCommandModule
{
    private const InteractionResponseType ResponseType = InteractionResponseType.ChannelMessageWithSource;

    private readonly CooldownService _cooldownService;
    private readonly SuggestionService _suggestionService;
    private readonly UserBlockingService _userBlockingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SuggestCommand" /> class.
    /// </summary>
    /// <param name="cooldownService">The <see cref="CooldownService" />.</param>
    /// <param name="suggestionService">The <see cref="SuggestionService" />.</param>
    /// <param name="userBlockingService">The <see cref="UserBlockingService" />.</param>
    public SuggestCommand(CooldownService cooldownService,
        SuggestionService suggestionService,
        UserBlockingService userBlockingService)
    {
        _cooldownService = cooldownService;
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
            builder.AsEphemeral();
            builder.WithContent("You are blocked from posting suggestions in this server.");
            await context.CreateResponseAsync(ResponseType, builder).ConfigureAwait(false);
            return;
        }

        TimeSpan cooldown = _cooldownService.GetRemainingCooldown(context.Member);
        if (cooldown > TimeSpan.Zero)
        {
            var builder = new DiscordInteractionResponseBuilder();
            builder.AsEphemeral();
            builder.WithContent($"You are on cooldown. You can suggest again in {cooldown.Humanize()}.");
            await context.CreateResponseAsync(ResponseType, builder).ConfigureAwait(false);
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
            followUp.AsEphemeral();
            await context.FollowUpAsync(followUp).ConfigureAwait(false);
            return;
        }

        Suggestion suggestion = _suggestionService.CreateSuggestion(context.Member, input.Value);
        DiscordMessage? message = await _suggestionService.PostSuggestionAsync(suggestion).ConfigureAwait(false);
        if (message == null)
        {
            followUp.WithContent("Failed to post suggestion. If this issue persists, please contact ModMail.");
            followUp.AsEphemeral();
            await context.FollowUpAsync(followUp).ConfigureAwait(false);
            return;
        }

        followUp.WithContent($"Your suggestion has been created and can be viewed here: {message.JumpLink}");
        followUp.AsEphemeral();
        await context.FollowUpAsync(followUp).ConfigureAwait(false);
    }
}
