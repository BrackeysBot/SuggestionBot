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
using SuggestionBot.Extensions;

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
    ///     Creates a staff-only embed for the specified suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion.</param>
    /// <returns>An embed for the suggestion.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="suggestion" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     The <see cref="Suggestion.Status" /> of <paramref name="suggestion" /> is not a valid value.
    /// </exception>
    public DiscordEmbed CreatePrivateEmbed(Suggestion suggestion)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (!_discordClient.Guilds.TryGetValue(suggestion.GuildId, out DiscordGuild? guild))
        {
            return new DiscordEmbedBuilder();
        }

        DiscordUser author = GetAuthor(suggestion);
        DiscordEmbedBuilder embed = CreateDefaultEmbed(suggestion, guild);
        embed.AddField("Author", author.Mention, true);
        embed.AddField("Submitted", Formatter.Timestamp(suggestion.Timestamp), true);
        embed.AddField("View Suggestion", GetSuggestionLink(suggestion), true);

        if (suggestion.ThreadId != 0 && suggestion.Status != SuggestionStatus.Removed)
        {
            embed.AddField("View Discussion", MentionUtility.MentionChannel(suggestion.ThreadId), true);
        }

        if (suggestion.StaffMemberId.HasValue)
        {
            embed.AddField("Approver", MentionUtility.MentionUser(suggestion.StaffMemberId.Value), true);
        }

        if (!string.IsNullOrWhiteSpace(suggestion.Remarks))
        {
            embed.AddField("Staff Remarks", suggestion.Remarks);
        }

        return embed;
    }

    /// <summary>
    ///     Creates a new embed for the specified suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion.</param>
    /// <returns>An embed for the suggestion.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="suggestion" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     The <see cref="Suggestion.Status" /> of <paramref name="suggestion" /> is not a valid value.
    /// </exception>
    public DiscordEmbed CreatePublicEmbed(Suggestion suggestion)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (!_discordClient.Guilds.TryGetValue(suggestion.GuildId, out DiscordGuild? guild))
        {
            return new DiscordEmbedBuilder();
        }

        DiscordEmbedBuilder embed = CreateDefaultEmbed(suggestion, guild);
        if (!string.IsNullOrWhiteSpace(suggestion.Remarks))
        {
            embed.AddField("Staff Remarks", suggestion.Remarks);
        }

        return embed;
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
            Id = GetNextId(guildId),
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
    ///     Gets the author of a suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion.</param>
    /// <returns>The author of the suggestion.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="suggestion" /> is <see langword="null" />.</exception>
    public DiscordUser GetAuthor(Suggestion suggestion)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (_discordClient.Guilds.TryGetValue(suggestion.GuildId, out DiscordGuild? guild))
        {
            if (guild.Members.TryGetValue(suggestion.AuthorId, out DiscordMember? member))
            {
                return member;
            }
        }

        // I hate doing GetAwaiter().GetResult() but I'd rather not make this method async so the embed
        // creation doesn't require an await. I truly hate this. forgive me father, for I have sinned.
        return _discordClient.GetUserAsync(suggestion.AuthorId).GetAwaiter().GetResult();
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
    ///     Gets the next available suggestion ID for the specified guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <returns>The next available suggestion ID.</returns>
    public long GetNextId(ulong guildId)
    {
        if (!_suggestions.TryGetValue(guildId, out List<Suggestion>? suggestions))
        {
            return 1;
        }

        long nextId = suggestions.Max(s => s.Id) + 1;

        while (TryGetSuggestion(guildId, nextId, out _))
        {
            nextId++;
        }

        return nextId;
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
    /// <returns>A read-only view of the suggestions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public IReadOnlyList<Suggestion> GetSuggestions(DiscordGuild guild)
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

        return suggestions.AsReadOnly();
    }

    /// <summary>
    ///     Returns the jump link for the specified suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion.</param>
    /// <returns>The jump link for the suggestion.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="suggestion" /> is <see langword="null" />.</exception>
    public Uri GetSuggestionLink(Suggestion suggestion)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        ulong guildId = suggestion.GuildId;
        ulong messageId = suggestion.MessageId;

        _configurationService.TryGetGuildConfiguration(guildId, out GuildConfiguration? configuration);
        return new Uri($"https://discord.com/channels/{guildId}/{configuration?.SuggestionChannel}/{messageId}");
    }

    /// <summary>
    ///     Returns the suggestion message for the specified suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion.</param>
    /// <returns>The message for the suggestion.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="suggestion" /> is <see langword="null" />.</exception>
    public DiscordMessage? GetSuggestionMessage(Suggestion suggestion)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        ulong guildId = suggestion.GuildId;
        ulong messageId = suggestion.MessageId;

        _configurationService.TryGetGuildConfiguration(guildId, out GuildConfiguration? configuration);
        configuration ??= new GuildConfiguration();

        if (!_discordClient.Guilds.TryGetValue(suggestion.GuildId, out DiscordGuild? guild))
        {
            return null;
        }

        DiscordChannel? channel = guild.GetChannel(configuration.SuggestionChannel);
        if (channel is null)
        {
            return null;
        }

        // once again, I hate doing GetAwaiter().GetResult(). but this is another sync method that I don't want to make
        // async purely to make an embed. I'm sorry.
        return channel.GetMessageAsync(messageId).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     Gets the discussion thread for the specified suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion.</param>
    /// <returns>The discussion thread for the suggestion, or <see langword="null" /> if it does not exist.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="suggestion" /> is <see langword="null" />.</exception>
    public DiscordThreadChannel? GetThread(Suggestion suggestion)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (suggestion.ThreadId == 0)
        {
            return null;
        }

        if (!_discordClient.Guilds.TryGetValue(suggestion.GuildId, out DiscordGuild? guild))
        {
            return null;
        }

        if (guild.Threads.TryGetValue(suggestion.ThreadId, out DiscordThreadChannel? threadChannel))
        {
            return threadChannel;
        }

        // this will probably always return null as threads are not stored in D#+ channel cache.
        // however, there may be an edge case where the suggestion's thread ID is its own channel,
        // so we shall use that as a last resort.
        return guild.GetChannel(suggestion.ThreadId) as DiscordThreadChannel;
    }

    /// <summary>
    ///     Gets the top-rated suggestions for the specified guild.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <param name="count">The number of suggestions to return.</param>
    /// <returns>A read-only view of the top-rated suggestions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public IReadOnlyList<Suggestion> GetTopSuggestions(DiscordGuild guild, int count = 10)
    {
        if (guild is null)
        {
            throw new ArgumentNullException(nameof(guild));
        }

        if (!_suggestions.TryGetValue(guild.Id, out List<Suggestion>? suggestions))
        {
            using SuggestionContext context = _contextFactory.CreateDbContext();
            suggestions = context.Suggestions
                .Where(s => s.GuildId == guild.Id && s.Status == SuggestionStatus.Suggested)
                .ToList();
            _suggestions.TryAdd(guild.Id, suggestions);
        }

        suggestions.Sort((s1, s2) => s2.Score.CompareTo(s1.Score));
        return suggestions.Take(count).ToArray();
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

        _logger.LogInformation("User {User} submitted suggestion {Id} in {Guild}", suggestion.AuthorId, suggestion.Id,
            guild);
        DiscordMessage message = await channel.SendMessageAsync(CreatePublicEmbed(suggestion)).ConfigureAwait(false);
        SetMessage(suggestion, message);

        await message.CreateReactionAsync(DiscordEmoji.FromUnicode("👍")).ConfigureAwait(false);
        await message.CreateReactionAsync(DiscordEmoji.FromUnicode("👎")).ConfigureAwait(false);

        if (configuration.CreateThreadForSuggestion)
        {
            const AutoArchiveDuration archiveDuration = AutoArchiveDuration.Week;

            DiscordUser author = GetAuthor(suggestion);
            var threadName = $"Suggestion from {author.GetUsernameWithDiscriminator()}";
            var thread = await message.CreateThreadAsync(threadName, archiveDuration).ConfigureAwait(false);

            DiscordMember? member = await author.GetAsMemberOfAsync(guild);
            if (member is not null)
            {
                await thread.AddThreadMemberAsync(member).ConfigureAwait(false);
            }

            SetThread(suggestion, thread);
        }

        return message;
    }

    /// <summary>
    ///     Sets the message of a suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion to update.</param>
    /// <param name="message">The new message of the suggestion.</param>
    /// <returns><see langword="true" /> if the message was updated; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="suggestion" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="message" /> is <see langword="null" />.</para>
    /// </exception>
    public bool SetMessage(Suggestion suggestion, DiscordMessage message)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (suggestion.MessageId == message.Id)
        {
            return false;
        }

        suggestion.MessageId = message.Id;
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
    /// <param name="staffMember">The staff member who updated the status.</param>
    /// <param name="remarks">Additional remarks about the suggestion.</param>
    /// <returns><see langword="true" /> if the status was updated; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="suggestion" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    public bool SetStatus(Suggestion suggestion,
        SuggestionStatus status,
        DiscordMember staffMember,
        string? remarks = null)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        if (suggestion.Status == status)
        {
            return false;
        }

        string humanizedStatus = status.Humanize(LetterCasing.AllCaps);
        string oldHumanizedStatus = suggestion.Status.Humanize(LetterCasing.AllCaps);

        if (!_configurationService.TryGetGuildConfiguration(suggestion.GuildId, out GuildConfiguration? configuration))
        {
            configuration = new GuildConfiguration();
        }

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(status switch
        {
            SuggestionStatus.Suggested => configuration.SuggestedColor,
            SuggestionStatus.Rejected => configuration.RejectedColor,
            SuggestionStatus.Implemented => configuration.ImplementedColor,
            SuggestionStatus.Accepted => configuration.AcceptedColor,
            SuggestionStatus.Removed => configuration.RemovedColor,
            _ => DiscordColor.CornflowerBlue
        });
        embed.WithTitle("Suggestion Status Updated");
        embed.WithDescription($"The status of suggestion {suggestion.Id} has been updated to **{humanizedStatus}**.");
        embed.AddField("Old Status", oldHumanizedStatus, true);
        embed.AddField("New Status", humanizedStatus, true);
        embed.AddField("Staff Member", staffMember.Mention, true);
        if (status != SuggestionStatus.Removed)
        {
            embed.AddField("View Suggestion", GetSuggestionLink(suggestion), true);
        }

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            embed.AddField("Staff Remarks", remarks);
        }

        _ = _logService.LogAsync(suggestion.GuildId, embed);

        suggestion.Status = status;
        suggestion.StaffMemberId = staffMember.Id;
        suggestion.Remarks = remarks;

        using SuggestionContext context = _contextFactory.CreateDbContext();
        context.Suggestions.Update(suggestion);
        context.SaveChanges();

        _logger.LogInformation("{StaffMember} marked suggestion {Id} as {Status}", staffMember, suggestion.Id,
            humanizedStatus);

        _ = UpdateSuggestionAsync(suggestion);
        return true;
    }

    /// <summary>
    ///     Sets the message of a suggestion.
    /// </summary>
    /// <param name="suggestion">The suggestion to update.</param>
    /// <param name="thread">The discussion thread.</param>
    /// <returns><see langword="true" /> if the message was updated; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="suggestion" /> is <see langword="null" />.</exception>
    public bool SetThread(Suggestion suggestion, DiscordThreadChannel? thread)
    {
        if (suggestion is null)
        {
            throw new ArgumentNullException(nameof(suggestion));
        }

        ulong threadId = thread?.Id ?? 0;
        if (suggestion.ThreadId == threadId)
        {
            return false;
        }

        suggestion.ThreadId = threadId;
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

        if (suggestion.Status == SuggestionStatus.Removed)
        {
            DiscordThreadChannel? thread = GetThread(suggestion);
            if (thread is not null)
            {
                await thread.DeleteAsync().ConfigureAwait(false);
            }

            await message.DeleteAsync();
            return;
        }

        DiscordEmbed embed = CreatePublicEmbed(suggestion);
        await message.ModifyAsync(m => m.Embed = embed).ConfigureAwait(false);

        if (suggestion.Status != SuggestionStatus.Suggested)
        {
            await message.DeleteAllReactionsAsync().ConfigureAwait(false);

            if (suggestion.Status != SuggestionStatus.Accepted)
            {
                DiscordThreadChannel? thread = GetThread(suggestion);
                if (thread is not null)
                {
                    await thread.DeleteAsync("Suggestion closed").ConfigureAwait(false);
                    SetThread(suggestion, null);
                }
            }
        }
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable += OnGuildAvailable;
        _discordClient.MessageReactionAdded += OnMessageReactionAdded;
        _discordClient.MessageReactionRemoved += OnMessageReactionRemoved;

        Load();
        return Task.CompletedTask;
    }

    private DiscordEmbedBuilder CreateDefaultEmbed(Suggestion suggestion, DiscordGuild guild)
    {
        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration))
        {
            configuration = new GuildConfiguration();
        }

        string emoji = suggestion.Status switch
        {
            SuggestionStatus.Suggested => "🗳️",
            SuggestionStatus.Rejected => "❌",
            SuggestionStatus.Implemented => "✅",
            SuggestionStatus.Accepted => "✅",
            SuggestionStatus.Duplicate => "🔁",
            SuggestionStatus.AlreadyImplemented => "✅",
            SuggestionStatus.AlreadyPlanned => "📅",
            _ => throw new ArgumentOutOfRangeException(nameof(suggestion), suggestion.Status, null)
        };

        DiscordUser author = GetAuthor(suggestion);
        var embed = new DiscordEmbedBuilder();
        string authorName = author.GetUsernameWithDiscriminator();
        embed.WithAuthor($"Suggestion from {authorName}", iconUrl: author.GetAvatarUrl(ImageFormat.Png));
        embed.WithThumbnail(guild.GetIconUrl(ImageFormat.Png));
        embed.WithDescription(suggestion.Content);
        embed.WithFooter($"Suggestion {suggestion.Id}");
        embed.WithTimestamp(suggestion.Timestamp);
        embed.WithColor(suggestion.Status switch
        {
            SuggestionStatus.Suggested => configuration.SuggestedColor,
            SuggestionStatus.Rejected => configuration.RejectedColor,
            SuggestionStatus.Implemented or SuggestionStatus.AlreadyImplemented => configuration.ImplementedColor,
            SuggestionStatus.Accepted => configuration.AcceptedColor,
            SuggestionStatus.Duplicate => configuration.DuplicateColor,
            SuggestionStatus.AlreadyPlanned => configuration.PlannedColor,
            _ => throw new ArgumentOutOfRangeException(nameof(suggestion), suggestion.Status, null)
        });

        embed.AddField("Status", $"{emoji} **{suggestion.Status.Humanize(LetterCasing.AllCaps)}**", true);
        return embed;
    }

    private Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs args)
    {
        string emoji = args.Emoji.Name;

        if (!ValidateReaction(args.User as DiscordMember, args.Message, emoji, out Suggestion? suggestion))
        {
            return Task.CompletedTask;
        }

        switch (emoji)
        {
            case "👍":
                suggestion.UpVotes--;
                break;

            case "👎":
                suggestion.DownVotes--;
                break;

            default:
                return Task.CompletedTask;
        }

        using SuggestionContext context = _contextFactory.CreateDbContext();
        context.Suggestions.Update(suggestion);
        context.SaveChanges();

        return Task.CompletedTask;
    }

    private Task OnMessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs args)
    {
        string emoji = args.Emoji.Name;

        if (!ValidateReaction(args.User as DiscordMember, args.Message, emoji, out Suggestion? suggestion))
        {
            return Task.CompletedTask;
        }

        switch (emoji)
        {
            case "👍":
                suggestion.UpVotes++;
                break;

            case "👎":
                suggestion.DownVotes++;
                break;

            default:
                return Task.CompletedTask;
        }

        using SuggestionContext context = _contextFactory.CreateDbContext();
        context.Suggestions.Update(suggestion);
        context.SaveChanges();

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

        RefreshSuggestionScores(args.Guild);
        return Task.CompletedTask;
    }

    private bool ValidateReaction(DiscordMember? member,
        DiscordMessage message,
        string emoji,
        [NotNullWhen(true)] out Suggestion? suggestion)
    {
        suggestion = null;

        if (member is null || member.IsBot)
        {
            return false;
        }

        DiscordGuild guild = member.Guild;
        if (message.Channel.Id != _configurationService.GetGuildConfiguration(guild)?.SuggestionChannel)
        {
            return false;
        }

        if (!_suggestions.TryGetValue(guild.Id, out List<Suggestion>? suggestions))
        {
            return false;
        }

        if (emoji != "👍" && emoji != "👎")
        {
            return false;
        }

        suggestion = suggestions.FirstOrDefault(s => s.MessageId == message.Id);
        if (suggestion is null)
        {
            return false;
        }

        return suggestion.Status != SuggestionStatus.Suggested;
    }

    private void RefreshSuggestionScores(DiscordGuild guild)
    {
        IEnumerable<Suggestion> suggestions = GetSuggestions(guild).Where(s => s is
        {
            Status: SuggestionStatus.Suggested,
            UpVotes: 0,
            DownVotes: 0
        });

        var updatedSuggestions = new List<Suggestion>();

        foreach (Suggestion suggestion in suggestions)
        {
            DiscordMessage? message = GetSuggestionMessage(suggestion);
            if (message is null)
            {
                continue;
            }

            int upVotes = message.Reactions.Count(r => r.Emoji.Name == "👍");
            int downVotes = message.Reactions.Count(r => r.Emoji.Name == "👎");
            if (upVotes == suggestion.UpVotes && downVotes == suggestion.DownVotes)
            {
                continue;
            }

            suggestion.UpVotes = upVotes;
            suggestion.DownVotes = downVotes;
            updatedSuggestions.Add(suggestion);
        }

        if (updatedSuggestions.Count == 0)
        {
            return;
        }

        using SuggestionContext context = _contextFactory.CreateDbContext();
        context.Suggestions.UpdateRange(updatedSuggestions);
        context.SaveChanges();
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
