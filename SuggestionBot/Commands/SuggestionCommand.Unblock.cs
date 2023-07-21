using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace SuggestionBot.Commands;

internal sealed partial class SuggestionCommand
{
    [SlashCommand("unblock", "Unblocks a user from posting suggestions.", false)]
    public async Task UnblockAsync(InteractionContext context,
        [Option("user", "The user to unblock.")] DiscordUser user)
    {
        _userBlockingService.UnblockUser(context.Guild, user, context.Member);

        var builder = new DiscordInteractionResponseBuilder();
        builder.WithContent($"The user {user.Mention} has been unblocked from posting suggestions.");
        builder.AsEphemeral();
        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder)
            .ConfigureAwait(false);
    }
}
