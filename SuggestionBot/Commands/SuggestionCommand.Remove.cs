using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SuggestionBot.AutocompleteProviders;
using SuggestionBot.Data;
using X10D.DSharpPlus;

namespace SuggestionBot.Commands;

internal sealed partial class SuggestionCommand
{
    [SlashCommand("remove", "Remove a suggestion.", false)]
    public async Task RemoveAsync(InteractionContext context,
        [Option("suggestion", "The suggestion whose status to change.")]
        [Autocomplete(typeof(SuggestionAutocompleteProvider))]
        string query,
        [Option("remarks", "Additional remarks about the suggestion.")]
        string? remarks = null)
    {
        var response = new DiscordInteractionResponseBuilder();

        if (!TryGetSuggestion(context.Guild, query, out Suggestion? suggestion))
        {
            response.AsEphemeral();
            response.AddEmbed(CreateNotFoundEmbed(query));
            await context.CreateResponseAsync(ResponseType, response).ConfigureAwait(false);
            return;
        }

        var embed = new DiscordEmbedBuilder();
        if (_suggestionService.SetStatus(suggestion, SuggestionStatus.Removed, context.Member, remarks))
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Suggestion Removed");
            embed.WithDescription($"The suggestion with the ID {suggestion.Id} has been REMOVED.");
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                embed.AddField("Staff Remarks", remarks);
            }

            await _mailmanService.SendSuggestionAsync(suggestion).ConfigureAwait(false);
        }

        response.AddEmbed(embed);
        await context.CreateResponseAsync(ResponseType, response).ConfigureAwait(false);
    }
}
