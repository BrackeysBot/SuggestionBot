using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SuggestionBot.AutocompleteProviders;
using SuggestionBot.Data;

namespace SuggestionBot.Commands;

internal sealed partial class SuggestionCommand
{
    [SlashCommand("reject", "Rejects a suggestion.", false)]
    public async Task RejectAsync(InteractionContext context,
        [Option("suggestion", "The suggestion to reject."), Autocomplete(typeof(SuggestionAutocompleteProvider))]
        string query)
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
        if (_suggestionService.Reject(suggestion, context.Member))
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("Suggestion Rejected");
            embed.WithDescription($"The suggestion with the ID {suggestion.Id} has been marked as REJECTED.");
        }
        else
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("Suggestion Unchanged");
            embed.WithDescription($"The suggestion with the ID {suggestion.Id} was already rejected. " +
                                  "No changes were made.");
        }

        response.AddEmbed(embed);
        await context.CreateResponseAsync(ResponseType, response).ConfigureAwait(false);
    }
}
