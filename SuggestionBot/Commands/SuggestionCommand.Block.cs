using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace SuggestionBot.Commands;

internal sealed partial class SuggestionCommand
{
    [SlashCommand("block", "Blocks a user from posting suggestions.", false)]
    public async Task BlockAsync(InteractionContext context,
        [Option("user", "The user to block.")] DiscordUser user,
        [Option("reason", "The reason for blocking the user.")]
        string? reason = null)
    {
        _userBlockingService.BlockUser(context.Guild, user, context.Member, reason);

        var builder = new DiscordInteractionResponseBuilder();
        builder.WithContent($"The user {user.Mention} has been blocked from posting suggestions.");
        builder.AsEphemeral();
        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder)
            .ConfigureAwait(false);
    }
}
