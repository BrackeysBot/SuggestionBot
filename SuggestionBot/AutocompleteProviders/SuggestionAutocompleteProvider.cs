using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using SuggestionBot.Data;
using SuggestionBot.Services;
using X10D.DSharpPlus;

namespace SuggestionBot.AutocompleteProviders;

internal sealed class SuggestionAutocompleteProvider : IAutocompleteProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext context)
    {
        const StringComparison comparison = StringComparison.OrdinalIgnoreCase;

        string query = context.OptionValue?.ToString() ?? string.Empty;
        bool hasQuery = !string.IsNullOrWhiteSpace(query);

        var choices = new List<DiscordAutoCompleteChoice>();
        var suggestionService = context.Services.GetRequiredService<SuggestionService>();

        if (hasQuery && query[0] == '#' && long.TryParse(query.AsSpan()[1..], out long id))
        {
            if (suggestionService.TryGetSuggestion(context.Guild.Id, id, out Suggestion? suggestion))
            {
                choices.Add(CreateChoice(suggestionService, suggestion));
            }
        }

        string[] queryWords = hasQuery ? query.Split() : Array.Empty<string>();
        IReadOnlyList<Suggestion> suggestions = suggestionService.GetSuggestions(context.Guild);

        int count = Math.Min(15, suggestions.Count);
        for (var index = 0; index < count; index++)
        {
            Suggestion suggestion = suggestions[index];
            DiscordUser author = suggestionService.GetAuthor(suggestion);
            string[] contentWords = suggestion.Content.Split();

            if (!hasQuery ||
                query[0] != '#' && contentWords.Any(w => queryWords.Any(q => w.StartsWith(q, comparison))) ||
                author.GetUsernameWithDiscriminator().StartsWith(query))
            {
                choices.Add(CreateChoice(suggestionService, suggestion));
            }
        }

        return Task.FromResult<IEnumerable<DiscordAutoCompleteChoice>>(choices);
    }

    private static DiscordAutoCompleteChoice CreateChoice(SuggestionService suggestionService, Suggestion suggestion)
    {
        string summary = GetSuggestionSummary(suggestionService, suggestion);
        return new DiscordAutoCompleteChoice(summary, suggestion.Id.ToString());
    }

    private static string GetSuggestionSummary(SuggestionService suggestionService, Suggestion suggestion)
    {
        DiscordUser author = suggestionService.GetAuthor(suggestion);

        int contentLength = suggestion.Content.Length;
        int min = Math.Min(20, contentLength);
        string suffix = min < contentLength ? " ..." : string.Empty;

        return $"#{suggestion.Id} by {author.GetUsernameWithDiscriminator()}: {suggestion.Content[..min]}{suffix}";
    }
}
