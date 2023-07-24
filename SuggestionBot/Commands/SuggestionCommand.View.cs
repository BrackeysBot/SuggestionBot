using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SuggestionBot.AutocompleteProviders;
using SuggestionBot.Data;

namespace SuggestionBot.Commands;

internal sealed partial class SuggestionCommand
{
    [SlashCommand("view", "Views a suggestion.", false)]
    public async Task ViewAsync(InteractionContext context,
        [Option("id", "The ID of the suggestion to view.")]
        [Autocomplete(typeof(SuggestionAutocompleteProvider))]
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

        DiscordEmbed embed = _suggestionService.CreatePrivateEmbed(suggestion);
        response.AddEmbed(embed);
        await context.CreateResponseAsync(ResponseType, response).ConfigureAwait(false);
    }
}
