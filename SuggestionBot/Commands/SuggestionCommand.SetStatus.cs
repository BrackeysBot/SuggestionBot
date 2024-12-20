﻿using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Humanizer;
using SuggestionBot.AutocompleteProviders;
using SuggestionBot.Data;
using SuggestionBot.Extensions;

namespace SuggestionBot.Commands;

internal sealed partial class SuggestionCommand
{
    [SlashCommand("setstatus", "Sets a new status for a suggestion.", false)]
    public async Task SetStatusAsync(InteractionContext context,
        [Option("suggestion", "The suggestion whose status to change.")]
        [Autocomplete(typeof(SuggestionAutocompleteProvider))]
        string query,
        [Option("status", "The new status of the suggestion.")]
        SuggestionStatus status,
        [Option("remarks", "Additional remarks about the suggestion.")]
        string? remarks = null)
    {
        var response = new DiscordInteractionResponseBuilder();

        if (!TryGetSuggestion(context.Guild, query, out Suggestion? suggestion) ||
            suggestion.Status == SuggestionStatus.Removed)
        {
            response.AsEphemeral();
            response.AddEmbed(CreateNotFoundEmbed(query));
            await context.CreateResponseAsync(ResponseType, response).ConfigureAwait(false);
            return;
        }

        var embed = new DiscordEmbedBuilder();
        embed.AddField("View Suggestion", _suggestionService.GetSuggestionLink(suggestion), true);

        string humanizedStatus = status.Humanize(LetterCasing.AllCaps);

        if (_suggestionService.SetStatus(suggestion, status, context.Member, remarks))
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("Suggestion Status Changed");
            embed.WithDescription($"The suggestion with the ID {suggestion.Id} has been marked as {humanizedStatus}.");
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                embed.AddField("Staff Remarks", remarks);
            }

            await _mailmanService.SendSuggestionAsync(suggestion).ConfigureAwait(false);
        }
        else
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("Suggestion Unchanged");
            embed.WithDescription($"The suggestion with the ID {suggestion.Id} was already {humanizedStatus}. " +
                                  "No changes were made.");
        }

        response.AddEmbed(embed);
        await context.CreateResponseAsync(ResponseType, response).ConfigureAwait(false);
    }
}
