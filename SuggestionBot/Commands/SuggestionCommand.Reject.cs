using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using NLog;
using SuggestionBot.AutocompleteProviders;
using SuggestionBot.Data;

namespace SuggestionBot.Commands;

internal sealed partial class SuggestionCommand
{
    [SlashCommand("reject", "Rejects a suggestion.", false)]
    public async Task RejectAsync(InteractionContext context,
        [Option("suggestion", "The suggestion to reject."), Autocomplete(typeof(SuggestionAutocompleteProvider))]
        string rawSuggestionId)
    {
        try
        {
            Suggestion? suggestion = null;
            ulong guildId = context.Guild.Id;

            if (long.TryParse(rawSuggestionId, out long suggestionId) &&
                _suggestionService.TryGetSuggestion(guildId, suggestionId, out suggestion))
            {
            }
            else if (ulong.TryParse(rawSuggestionId, out ulong messageId) &&
                     _suggestionService.TryGetSuggestion(guildId, messageId, out suggestion))
            {
            }

            var response = new DiscordInteractionResponseBuilder();
            if (suggestion is null)
            {
                response.WithContent("The suggestion could not be found.");
            }
            else
            {
                _suggestionService.UpdateSuggestionStatus(suggestion, SuggestionStatus.Rejected, context.Member);
                await _suggestionService.UpdateSuggestionAsync(suggestion).ConfigureAwait(false);
                response.WithContent("The suggestion has been updated.");
            }

            await context.CreateResponseAsync(ResponseType, response).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogManager.GetCurrentClassLogger().Error(exception);
        }
    }
}
