using System.Diagnostics.CodeAnalysis;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using SuggestionBot.Data;
using SuggestionBot.Services;

namespace SuggestionBot.Commands;

[SlashCommandGroup("suggestion", "Manage suggestions.", false)]
[SlashRequireGuild]
internal sealed partial class SuggestionCommand : ApplicationCommandModule
{
    private const InteractionResponseType ResponseType = InteractionResponseType.ChannelMessageWithSource;

    private readonly SuggestionService _suggestionService;
    private readonly UserBlockingService _userBlockingService;
    private readonly MailmanService _mailmanService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SuggestionCommand" /> class.
    /// </summary>
    /// <param name="suggestionService">The <see cref="SuggestionService" />.</param>
    /// <param name="userBlockingService">The <see cref="UserBlockingService" />.</param>
    /// <param name="mailmanService">The <see cref="MailmanService" />.</param>
    public SuggestionCommand(SuggestionService suggestionService,
        UserBlockingService userBlockingService,
        MailmanService mailmanService)
    {
        _suggestionService = suggestionService;
        _userBlockingService = userBlockingService;
        _mailmanService = mailmanService;
    }

    private static DiscordEmbed CreateNotFoundEmbed(string query)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithTitle("Suggestion Not Found");
        embed.WithDescription($"The suggestion with the ID {query} could not be found.");
        return embed.Build();
    }

    private bool TryGetSuggestion(DiscordGuild guild, string query, [NotNullWhen(true)] out Suggestion? suggestion)
    {
        if (guild is null)
        {
            throw new ArgumentNullException(nameof(guild));
        }

        ulong guildId = guild.Id;

        if (long.TryParse(query, out long id) && _suggestionService.TryGetSuggestion(guildId, id, out suggestion))
        {
            return true;
        }

        if (ulong.TryParse(query, out ulong messageId) &&
            _suggestionService.TryGetSuggestion(guildId, messageId, out suggestion))
        {
            return true;
        }

        suggestion = null;
        return false;
    }
}
