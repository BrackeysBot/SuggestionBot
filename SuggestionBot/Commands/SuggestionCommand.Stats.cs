using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SuggestionBot.Data;
using static SuggestionBot.Data.SuggestionStatus;

namespace SuggestionBot.Commands;

internal sealed partial class SuggestionCommand
{
    [SlashCommand("stats", "View suggestion statistics.", false)]
    public async Task StatsAsync(InteractionContext context)
    {
        await context.DeferAsync().ConfigureAwait(false);
        var response = new DiscordWebhookBuilder();

        IReadOnlyList<Suggestion> suggestions = _suggestionService.GetSuggestions(context.Guild);
        int totalSuggestions = suggestions.Count;
        int openCounts = suggestions.Count(s => s.Status == Suggested);
        int rejectedCount = suggestions.Count(s => s.Status == Rejected);
        int implementedCount = suggestions.Count(s => s.Status is Implemented or AlreadyImplemented);
        int removedCount = suggestions.Count(s => s.Status == Removed);
        ulong highestContributor = suggestions
            .GroupBy(s => s.AuthorId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Blurple);
        embed.WithTitle("Suggestion Statistics");
        embed.AddField("Total", $"{totalSuggestions:N0}", true);
        embed.AddField("Open", $"{openCounts:N0}", true);
        embed.AddField("Rejected", $"{rejectedCount:N0}", true);
        embed.AddField("Implemented", $"{implementedCount:N0}", true);
        embed.AddField("Removed", $"{removedCount:N0}", true);
        embed.AddField("Highest Contributor", MentionUtility.MentionUser(highestContributor), true);

        response.AddEmbed(embed);
        await context.EditResponseAsync(response).ConfigureAwait(false);
    }
}
