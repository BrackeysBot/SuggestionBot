using System.Text;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SuggestionBot.Data;

namespace SuggestionBot.Commands;

using MentionUtility = X10D.DSharpPlus.MentionUtility;

internal sealed partial class SuggestionCommand
{
    [SlashCommand("top", "Views the top rated suggestions.", false)]
    public async Task TopAsync(InteractionContext context,
        [Option("count", "The number of suggestions to return.")]
        long count = 10)
    {
        await context.DeferAsync().ConfigureAwait(false);
        var response = new DiscordWebhookBuilder();
        var embed = new DiscordEmbedBuilder();

        IReadOnlyList<Suggestion> topSuggestions = _suggestionService.GetTopSuggestions(context.Guild, (int)count);
        if (topSuggestions.Count == 0)
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Top Suggestions");
            embed.WithDescription("⚠️ No open suggestions were found in this guild that match the criteria.");

            response.AddEmbed(embed);
            await context.EditResponseAsync(response).ConfigureAwait(false);
            return;
        }

        var builder = new StringBuilder();
        foreach (Suggestion suggestion in topSuggestions)
        {
            var messageLink = _suggestionService.GetSuggestionLink(suggestion).ToString();
            string author = MentionUtility.MentionUser(suggestion.AuthorId);
            builder.AppendLine($"• **{suggestion.Id} by {author}** ({suggestion.Score:+#;-#;0}) {messageLink}");
        }

        embed.WithColor(DiscordColor.Blurple);
        embed.WithTitle("Top Suggestions");
        embed.WithDescription($"__{topSuggestions.Count:N0} results__\n\n{builder}");

        response.AddEmbed(embed);
        await context.EditResponseAsync(response).ConfigureAwait(false);
    }
}
