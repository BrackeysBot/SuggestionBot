using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using SuggestionBot.Data;
using SuggestionBot.Services;

namespace SuggestionBot.AutocompleteProviders;

internal sealed class SuggestionAutocompleteProvider : IAutocompleteProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext context)
    {
        var suggestionService = context.Services.GetRequiredService<SuggestionService>();
        IEnumerable<Suggestion> suggestions = suggestionService.GetSuggestions(context.Guild, true);

        return Task.FromResult(suggestions.OrderByDescending(i => i.Timestamp).Take(10).Select(suggestion =>
        {
            string summary = GetSuggestionSummary(suggestion);
            return new DiscordAutoCompleteChoice(summary, suggestion.Id);
        }));
    }

    private static string GetSuggestionSummary(Suggestion suggestion)
    {
        string content = suggestion.Content[..Math.Min(20, suggestion.Content.Length)];
        return $"{suggestion.Id:N} by {suggestion.AuthorId}: {content}...";
    }
}
