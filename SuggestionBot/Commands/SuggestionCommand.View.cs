using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SuggestionBot.Data;

namespace SuggestionBot.Commands;

internal sealed partial class SuggestionCommand
{
    [SlashCommand("view", "Views a suggestion.", false)]
    public async Task ViewAsync(InteractionContext context,
        [Option("id", "The ID of the suggestion to view.")]
        string idRaw)
    {
        Suggestion? suggestion = null;

        ulong guildId = context.Guild.Id;
        if (ulong.TryParse(idRaw, out ulong messageId) &&
            _suggestionService.TryGetSuggestion(guildId, messageId, out suggestion))
        {
        }
        else if (long.TryParse(idRaw, out long id) && _suggestionService.TryGetSuggestion(guildId, id, out suggestion))
        {
        }

        var builder = new DiscordInteractionResponseBuilder();

        if (suggestion is null)
        {
            builder.WithContent($"The suggestion with ID {idRaw} does not exist.");
            builder.AsEphemeral();
            await context.CreateResponseAsync(ResponseType, builder).ConfigureAwait(false);
            return;
        }

        DiscordEmbed embed = _suggestionService.CreatePublicEmbed(suggestion);
        builder.AddEmbed(embed);
        await context.CreateResponseAsync(ResponseType, builder).ConfigureAwait(false);
    }
}
