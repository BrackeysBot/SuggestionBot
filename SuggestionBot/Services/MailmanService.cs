using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using SmartFormat;
using SuggestionBot.Configuration;
using SuggestionBot.Data;
using SuggestionBot.Resources;
using X10D.DSharpPlus;

namespace SuggestionBot.Services;

internal sealed class MailmanService
{
    private readonly DiscordClient _discordClient;
    private readonly ConfigurationService _configurationService;
    private readonly SuggestionService _suggestionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MailmanService" /> class.
    /// </summary>
    /// <param name="discordClient">The <see cref="DiscordClient" />.</param>
    /// <param name="configurationService">The <see cref="ConfigurationService" />.</param>
    /// <param name="suggestionService">The <see cref="SuggestionService" />.</param>
    public MailmanService(DiscordClient discordClient,
        ConfigurationService configurationService,
        SuggestionService suggestionService)
    {
        _discordClient = discordClient;
        _configurationService = configurationService;
        _suggestionService = suggestionService;
    }

    /// <summary>
    ///     Sends a suggestion to the author via DM.
    /// </summary>
    /// <param name="suggestion">The <see cref="Suggestion" /> to send.</param>
    /// <exception cref="ArgumentNullException"><paramref name="suggestion" /> is <see langword="null" />.</exception>
    public async Task SendSuggestionAsync(Suggestion suggestion)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (!_discordClient.Guilds.TryGetValue(suggestion.GuildId, out DiscordGuild? guild))
        {
            return;
        }

        DiscordUser author = _suggestionService.GetAuthor(suggestion);
        DiscordMember? member = await author.GetAsMemberOfAsync(guild).ConfigureAwait(false);
        if (member is null)
        {
            return;
        }

        Uri suggestionLink = _suggestionService.GetSuggestionLink(suggestion);
        string iconUrl = guild.GetIconUrl(ImageFormat.Png);

        var embed = new DiscordEmbedBuilder();
        embed.WithThumbnail(iconUrl);
        embed.WithFooter(guild.Name, iconUrl);

        if (!TryBuildEmbed(suggestion, embed, author, guild))
        {
            return;
        }

        suggestion.Content = suggestion.Content.Truncate(250, "...");
        embed.AddField("Your Suggestion", suggestion.Content);

        if (!string.IsNullOrWhiteSpace(suggestion.Remarks))
        {
            embed.AddField("Staff Remarks", suggestion.Remarks);
        }

        await member.SendMessageAsync(embed).ConfigureAwait(false);
    }

    private bool TryBuildEmbed(Suggestion suggestion, DiscordEmbedBuilder embed, DiscordUser author, DiscordGuild guild)
    {
        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration))
        {
            configuration = new GuildConfiguration();
        }

        switch (suggestion.Status)
        {
            case SuggestionStatus.Rejected:
                embed.WithColor(configuration.RejectedColor);
                embed.WithTitle("Suggestion Rejected");
                embed.WithDescription(PrivateMessages.RejectedDescription.FormatSmart(new { user = author, guild }));
                break;

            case SuggestionStatus.Accepted:
                embed.WithColor(configuration.AcceptedColor);
                embed.WithTitle("Suggestion Accepted");
                embed.WithDescription(PrivateMessages.AcceptedDescription.FormatSmart(new { user = author, guild }));
                break;

            case SuggestionStatus.Implemented:
                embed.WithColor(configuration.ImplementedColor);
                embed.WithTitle("Suggestion Implemented");
                embed.WithDescription(PrivateMessages.ImplementedDescription.FormatSmart(new { user = author, guild }));
                break;

            default:
                return false;
        }

        return true;
    }
}
