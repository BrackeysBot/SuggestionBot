using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SuggestionBot.AutocompleteProviders;
using SuggestionBot.Data;

namespace SuggestionBot.Commands;

internal sealed partial class SuggestionCommand
{
    [SlashCommand("implement", "Implements a suggestion.", false)]
    public async Task ImplementAsync(InteractionContext context,
        [Option("suggestion", "The suggestion to implement."), Autocomplete(typeof(SuggestionAutocompleteProvider))]
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
        if (_suggestionService.Implement(suggestion, context.Member))
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("Suggestion Implemented");
            embed.WithDescription($"The suggestion with the ID {suggestion.Id:N} has been marked as IMPLEMENTED.");
        }
        else
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("Suggestion Unchanged");
            embed.WithDescription($"The suggestion with the ID {suggestion.Id:N} was already implemented. " +
                                  "No changes were made.");
        }

        response.AddEmbed(embed);
        await context.CreateResponseAsync(ResponseType, response).ConfigureAwait(false);
    }
}
