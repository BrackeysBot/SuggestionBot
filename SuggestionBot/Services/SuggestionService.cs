using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuggestionBot.Configuration;
using SuggestionBot.Data;
using X10D.DSharpPlus;

namespace SuggestionBot.Services;

internal sealed class SuggestionService : BackgroundService
{
    private readonly ConcurrentDictionary<ulong, List<Suggestion>> _suggestions = new();
    private readonly ILogger<SuggestionService> _logger;
    private readonly IDbContextFactory<SuggestionContext> _contextFactory;
    private readonly DiscordClient _discordClient;
    private readonly ConfigurationService _configurationService;
    private readonly DiscordLogService _logService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SuggestionService" /> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}" />.</param>
    /// <param name="contextFactory">The <see cref="IDbContextFactory{TContext}" />.</param>
    /// <param name="discordClient">The <see cref="DiscordClient" />.</param>
    /// <param name="configurationService">The <see cref="ConfigurationService" />.</param>
    /// <param name="logService">The <see cref="DiscordLogService" />.</param>
    public SuggestionService(ILogger<SuggestionService> logger,
        IDbContextFactory<SuggestionContext> contextFactory,
        DiscordClient discordClient,
        ConfigurationService configurationService,
        DiscordLogService logService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _discordClient = discordClient;
        _configurationService = configurationService;
        _logService = logService;
    }

    /// <summary>
    ///     Creates a new suggestion.
    /// </summary>
    /// <param name="member">The member who created the suggestion.</param>
    /// <param name="content">The suggestion content.</param>
    /// <returns>The created suggestion.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="member" /> or <paramref name="content" /> is <see langword="null" />.
    /// </exception>
    public Suggestion CreateSuggestion(DiscordMember member, string content)
    {
        if (member == null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        ulong guildId = member.Guild.Id;

        var suggestion = new Suggestion
        {
            AuthorId = member.Id,
            Content = content,
            GuildId = guildId
        };

        _suggestions.AddOrUpdate(guildId, new List<Suggestion> { suggestion }, (_, suggestions) =>
        {
            suggestions.Add(suggestion);
            return suggestions;
        });

        using SuggestionContext context = _contextFactory.CreateDbContext();
        context.Suggestions.Add(suggestion);
        context.SaveChanges();

        _logger.LogInformation("Created suggestion {SuggestionId} in {Guild}.", suggestion.Id, member.Guild);
        return suggestion;
    }

    /// <summary>
    ///     Gets the last time a user made a suggestion in the specified guild.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <returns>The last time the user made a suggestion.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="member" /> is <see langword="null" />.</exception>
    public DateTimeOffset GetLastSuggestionTime(DiscordMember member)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        Suggestion[] userSuggestions = GetSuggestionsBy(member);
        return userSuggestions.Length == 0 ? DateTimeOffset.MinValue : userSuggestions.Max(s => s.Timestamp);
    }

    /// <summary>
    ///     Gets all suggestions made by the specified member.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <returns>An array of suggestions made by the member.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="member" /> is <see langword="null" />.</exception>
    public Suggestion[] GetSuggestionsBy(DiscordMember member)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        ulong guildId = member.Guild.Id;
        ulong userId = member.Id;

        return _suggestions.TryGetValue(guildId, out List<Suggestion>? suggestions)
            ? suggestions.FindAll(s => s.GuildId == guildId && s.AuthorId == userId).ToArray()
            : Array.Empty<Suggestion>();
    }

    /// <summary>
    ///     Gets the suggestions for the specified guild.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <param name="onlyReturnOpen">Whether to only return open suggestions.</param>
    /// <returns>A read-only view of the suggestions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public IReadOnlyList<Suggestion> GetSuggestions(DiscordGuild guild, bool onlyReturnOpen = false)
    {
        if (guild is null)
        {
            throw new ArgumentNullException(nameof(guild));
        }

        if (!_suggestions.TryGetValue(guild.Id, out List<Suggestion>? suggestions))
        {
            using SuggestionContext context = _contextFactory.CreateDbContext();
            suggestions = context.Suggestions.Where(s => s.GuildId == guild.Id).ToList();
            _suggestions.TryAdd(guild.Id, suggestions);
        }

        if (onlyReturnOpen)
        {
            suggestions = suggestions.Where(s => s.Status == SuggestionStatus.Suggested).ToList();
        }

        return suggestions.AsReadOnly();
    }

    /// <summary>
    ///     Marks a suggestion as implemented, and optionally updates the remarks for the implementation.
    /// </summary>
    /// <param name="suggestion">The suggestion to update.</param>
    /// <param name="staffMember">The staff member who implemented the suggestion.</param>
    /// <returns><see langword="true" /> if the status was updated; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="suggestion" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    public bool Implement(Suggestion suggestion, DiscordMember staffMember)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (staffMember is null)
        {
            throw new ArgumentNullException(nameof(staffMember));
        }

        if (!SetStatus(suggestion, SuggestionStatus.Implemented))
        {
            return false;
        }

        _logger.LogInformation("{StaffMember} marked suggestion {Id} as IMPLEMENTED", staffMember, suggestion.Id);

        suggestion.StaffMemberId = staffMember.Id;

        using SuggestionContext context = _contextFactory.CreateDbContext();
        context.Suggestions.Update(suggestion);
        context.SaveChanges();
        return true;
    }

    /// <summary>
    ///     Posts a suggestion to the suggestion channel of the guild in which it was made.
    /// </summary>
    /// <param name="suggestion">The suggestion to post.</param>
    /// <exception cref="ArgumentNullException"><paramref name="suggestion" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="suggestion" />.<see cref="Suggestion.Status" /> is not a valid value.
    /// </exception>
    public async Task<DiscordMessage?> PostSuggestionAsync(Suggestion suggestion)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (!_discordClient.Guilds.TryGetValue(suggestion.GuildId, out DiscordGuild? guild))
        {
            _logger.LogTrace("Guild {GuildId} does not exist", suggestion.GuildId);
            return null;
        }

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration))
        {
            _logger.LogTrace("{Guild} is not configured", guild);
            return null;
        }

        if (guild.GetChannel(configuration.SuggestionChannel) is not { } channel)
        {
            _logger.LogTrace("Channel {ChannelId} does not exist in {Guild}", configuration.SuggestionChannel, guild);
            return null;
        }

        DiscordMessage message = await channel.SendMessageAsync("...").ConfigureAwait(false);
        UpdateSuggestionMessage(suggestion, message);
        await UpdateSuggestionAsync(suggestion).ConfigureAwait(false);

        await message.CreateReactionAsync(DiscordEmoji.FromUnicode("👍")).ConfigureAwait(false);
        await message.CreateReactionAsync(DiscordEmoji.FromUnicode("👎")).ConfigureAwait(false);
        return message;
    }

    /// <summary>
    ///     Marks a suggestion as rejected, and updates the reason for the rejection if one is provided.
    /// </summary>
    /// <param name="suggestion">The suggestion to update.</param>
    /// <param name="staffMember">The staff member who rejected the suggestion.</param>
    /// <returns><see langword="true" /> if the status was updated; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="suggestion" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    public bool Reject(Suggestion suggestion, DiscordMember staffMember)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (staffMember is null)
        {
            throw new ArgumentNullException(nameof(staffMember));
        }

        if (!SetStatus(suggestion, SuggestionStatus.Rejected))
        {
            return false;
        }

        _logger.LogInformation("{StaffMember} marked suggestion {Id} as REJECTED", staffMember, suggestion.Id);

        suggestion.StaffMemberId = staffMember.Id;

        using SuggestionContext context = _contextFactory.CreateDbContext();
        context.Suggestions.Update(suggestion);
        context.SaveChanges();
        return true;
    }

    /// <summary>
    ///     Sets the status of a suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion to update.</param>
    /// <param name="status">The new status of the suggestion.</param>
    /// <returns><see langword="true" /> if the status was updated; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="suggestion" /> is <see langword="null" />.</exception>
    public bool SetStatus(Suggestion suggestion, SuggestionStatus status)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (suggestion.Status == status)
        {
            return false;
        }

        suggestion.Status = status;
        using SuggestionContext context = _contextFactory.CreateDbContext();
        context.Suggestions.Update(suggestion);
        context.SaveChanges();
        return true;
    }

    /// <summary>
    ///     Attempts to get a suggestion by its message ID.
    /// </summary>
    /// <param name="guildId">The ID of the guild in which the suggestion was made.</param>
    /// <param name="messageId">The message ID of the suggestion.</param>
    /// <param name="suggestion">
    ///     When this method returns, contains the suggestion if it was found; otherwise, <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if the suggestion was found; otherwise, <see langword="false" />.</returns>
    public bool TryGetSuggestion(ulong guildId, ulong messageId, [NotNullWhen(true)] out Suggestion? suggestion)
    {
        if (_suggestions.TryGetValue(guildId, out List<Suggestion>? suggestions))
        {
            suggestion = suggestions.FirstOrDefault(s => s.MessageId == messageId);
            return suggestion != null;
        }

        suggestion = null;
        return false;
    }

    /// <summary>
    ///     Attempts to get a suggestion by its ID.
    /// </summary>
    /// <param name="guildId">The ID of the guild in which the suggestion was made.</param>
    /// <param name="id">The ID of the suggestion.</param>
    /// <param name="suggestion">
    ///     When this method returns, contains the suggestion if it was found; otherwise, <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if the suggestion was found; otherwise, <see langword="false" />.</returns>
    public bool TryGetSuggestion(ulong guildId, long id, [NotNullWhen(true)] out Suggestion? suggestion)
    {
        if (_suggestions.TryGetValue(guildId, out List<Suggestion>? suggestions))
        {
            suggestion = suggestions.FirstOrDefault(s => s.Id == id);
            return suggestion != null;
        }

        suggestion = null;
        return false;
    }

    /// <summary>
    ///     Updates the message of a suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion to update.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="suggestion" /> or <paramref name="message" /> is <see langword="null" />.
    /// </exception>
    public async Task UpdateSuggestionAsync(Suggestion suggestion)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (suggestion.MessageId == 0)
        {
            return;
        }

        if (!_discordClient.Guilds.TryGetValue(suggestion.GuildId, out DiscordGuild? guild))
        {
            return;
        }

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration))
        {
            return;
        }

        DiscordChannel? channel = guild.GetChannel(configuration.SuggestionChannel);
        if (channel is null)
        {
            return;
        }

        DiscordMessage? message = await channel.GetMessageAsync(suggestion.MessageId).ConfigureAwait(false);
        if (message is null)
        {
            return;
        }

        DiscordEmbed embed = await GetSuggestionEmbedAsync(suggestion).ConfigureAwait(false);
        await message.ModifyAsync(m => m.Embed = embed).ConfigureAwait(false);

        if (suggestion.Status != SuggestionStatus.Suggested)
        {
            await message.DeleteAllReactionsAsync().ConfigureAwait(false);
        }
    }

    public async Task<DiscordEmbed> GetSuggestionEmbedAsync(Suggestion suggestion)
    {
        if (!_discordClient.Guilds.TryGetValue(suggestion.GuildId, out DiscordGuild? guild))
        {
            return new DiscordEmbedBuilder();
        }

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration))
        {
            configuration = new GuildConfiguration();
        }

        DiscordUser author = await _discordClient.GetUserAsync(suggestion.AuthorId).ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        string authorName = author.GetUsernameWithDiscriminator();
        embed.WithAuthor($"Suggestion from {authorName}", iconUrl: author.GetAvatarUrl(ImageFormat.Png));
        embed.WithThumbnail(guild.GetIconUrl(ImageFormat.Png));
        embed.WithColor(suggestion.Status switch
        {
            SuggestionStatus.Suggested => configuration.SuggestedColor,
            SuggestionStatus.Rejected => configuration.RejectedColor,
            SuggestionStatus.Implemented => configuration.ImplementedColor,
            _ => throw new ArgumentOutOfRangeException(nameof(suggestion), suggestion.Status, null)
        });

        embed.WithDescription(suggestion.Content);
        embed.WithFooter($"Suggestion {suggestion.Id}");

        string emoji = suggestion.Status switch
        {
            SuggestionStatus.Suggested => "🗳️",
            SuggestionStatus.Rejected => "❌",
            SuggestionStatus.Implemented => "✅",
            _ => throw new ArgumentOutOfRangeException(nameof(suggestion), suggestion.Status, null)
        };

        embed.AddField("Status", $"{emoji} **{suggestion.Status.Humanize(LetterCasing.AllCaps)}**", true);
        return embed;
    }

    /// <summary>
    ///     Updates the message of a suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion to update.</param>
    /// <param name="message">The new message of the suggestion.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="suggestion" /> or <paramref name="message" /> is <see langword="null" />.
    /// </exception>
    public void UpdateSuggestionMessage(Suggestion suggestion, DiscordMessage message)
    {
        if (suggestion is null) throw new ArgumentNullException(nameof(suggestion));
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (suggestion.MessageId != 0) return;

        suggestion.MessageId = message.Id;

        using SuggestionContext context = _contextFactory.CreateDbContext();
        context.Suggestions.Update(suggestion);
        context.SaveChanges();
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable += OnGuildAvailable;
        Load();
        return Task.CompletedTask;
    }

    private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs args)
    {
        DiscordGuild guild = args.Guild;

        if (_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration))
        {
            DiscordChannel? logChannel = guild.GetChannel(configuration.LogChannel);
            if (logChannel is null)
            {
                _logger.LogWarning("Log channel {LogChannel} does not exist in {Guild}", configuration.LogChannel,
                    guild);
            }
            else
            {
                _logger.LogInformation("{Channel} found in {Guild}", logChannel, guild);
            }
        }
        else
        {
            _logger.LogWarning("{Guild} is not configured!", guild);
        }

        return Task.CompletedTask;
    }

    private void Load()
    {
        using SuggestionContext context = _contextFactory.CreateDbContext();
        foreach (IGrouping<ulong, Suggestion> group in context.Suggestions.GroupBy(s => s.GuildId))
        {
            _suggestions.AddOrUpdate(group.Key, _ => group.ToList(), (_, suggestions) =>
            {
                suggestions.AddRange(group);
                return suggestions;
            });
        }

        _logger.LogInformation("Loaded {SuggestionCount} suggestions", _suggestions.Sum(s => s.Value.Count));
    }
}
