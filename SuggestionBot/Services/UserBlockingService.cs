using System.Collections.Concurrent;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuggestionBot.Data;
using SuggestionBot.Extensions;

namespace SuggestionBot.Services;

internal sealed class UserBlockingService : BackgroundService
{
    private readonly ILogger<UserBlockingService> _logger;
    private readonly IDbContextFactory<SuggestionContext> _dbContextFactory;
    private readonly DiscordLogService _logService;

    private readonly ConcurrentDictionary<ulong, List<ulong>> _blockedUsers = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserBlockingService" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="logService">The log service.</param>
    public UserBlockingService(ILogger<UserBlockingService> logger,
        IDbContextFactory<SuggestionContext> dbContextFactory,
        DiscordLogService logService)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _logService = logService;
    }

    /// <summary>
    ///     Blocks a user from using the bot in the specified guild.
    /// </summary>
    /// <param name="member">The member to block.</param>
    /// <param name="staffMember">The staff member who blocked the user.</param>
    /// <param name="reason">The reason for blocking the user.</param>
    public void BlockUser(DiscordMember member, DiscordMember staffMember, string? reason)
    {
        BlockUser(member.Guild, member, staffMember, reason);
    }

    /// <summary>
    ///     Blocks a user from using the bot in the specified guild.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <param name="user">The user to block.</param>
    /// <param name="staffMember">The staff member who blocked the user.</param>
    /// <param name="reason">The reason for blocking the user.</param>
    public void BlockUser(DiscordGuild guild, DiscordUser user, DiscordMember staffMember, string? reason)
    {
        _blockedUsers.AddOrUpdate(guild.Id, _ => new List<ulong> { user.Id }, (_, list) =>
        {
            list.Add(user.Id);
            return list;
        });

        using SuggestionContext context = _dbContextFactory.CreateDbContext();
        context.BlockedUsers.Add(new BlockedUser
        {
            GuildId = guild.Id,
            UserId = user.Id,
            StaffMemberId = staffMember.Id,
            Reason = reason,
            Timestamp = DateTimeOffset.UtcNow
        });

        context.SaveChanges();

        _logger.LogInformation("{StaffMember} blocked user {User} in {Guild}. Reason: {Reason}", staffMember, user,
            guild, reason ?? "None");

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);
        embed.WithTitle("User Blocked");
        embed.WithColor(DiscordColor.Red);
        embed.WithTimestamp(DateTimeOffset.UtcNow);
        embed.WithDescription($"The user {user.Mention} has been blocked from posting suggestions.");
        embed.AddField("Staff Member", staffMember.Mention, true);
        embed.AddField("Reason", reason ?? "None", true);

        _ = _logService.LogAsync(guild, embed);
    }

    /// <summary>
    ///     Returns a value indicating whether the specified member is blocked from using the bot.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <returns><see langword="true" /> if the member is blocked; otherwise, <see langword="false" />.</returns>
    public bool IsUserBlocked(DiscordMember member)
    {
        return IsUserBlocked(member.Guild, member);
    }

    /// <summary>
    ///     Returns a value indicating whether the specified user is blocked from using the bot in the specified guild.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <param name="user">The user.</param>
    /// <returns><see langword="true" /> if the user is blocked; otherwise, <see langword="false" />.</returns>
    public bool IsUserBlocked(DiscordGuild guild, DiscordUser user)
    {
        return _blockedUsers.TryGetValue(guild.Id, out List<ulong>? blockedUsers) && blockedUsers.Contains(user.Id);
    }

    /// <summary>
    ///     Unblocks a user from using the bot in the specified guild.
    /// </summary>
    /// <param name="member">The member to unblock.</param>
    /// <param name="staffMember">The staff member who unblocked the user.</param>
    public void UnblockUser(DiscordMember member, DiscordMember staffMember)
    {
        UnblockUser(member.Guild, member, staffMember);
    }

    /// <summary>
    ///     Unblocks a user from using the bot in the specified guild.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <param name="user">The user to unblock.</param>
    /// <param name="staffMember">The staff member who unblocked the user.</param>
    public void UnblockUser(DiscordGuild guild, DiscordUser user, DiscordMember staffMember)
    {
        if (_blockedUsers.TryGetValue(guild.Id, out List<ulong>? blockedUsers))
        {
            blockedUsers.Remove(user.Id);
        }

        using SuggestionContext context = _dbContextFactory.CreateDbContext();
        BlockedUser? blockedUser = context.BlockedUsers.Find(guild.Id, user.Id);
        if (blockedUser is not null)
        {
            context.BlockedUsers.Remove(blockedUser);
            context.SaveChanges();
        }

        _logger.LogInformation("{StaffMember} unblocked user {user} in {Guild}", staffMember, user, guild);

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);
        embed.WithTitle("User Unblocked");
        embed.WithColor(DiscordColor.Green);
        embed.WithTimestamp(DateTimeOffset.UtcNow);
        embed.WithDescription($"The user {user.Mention} has been unblocked from posting suggestions.");
        embed.AddField("Staff Member", staffMember.Mention, true);

        _ = _logService.LogAsync(guild, embed);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Load();
        return Task.CompletedTask;
    }

    private void Load()
    {
        using SuggestionContext context = _dbContextFactory.CreateDbContext();
        foreach (IGrouping<ulong, BlockedUser> group in context.BlockedUsers.GroupBy(u => u.GuildId))
        {
            IEnumerable<ulong> userIds = group.Select(u => u.UserId);
            _blockedUsers.AddOrUpdate(group.Key, _ => userIds.ToList(), (_, list) =>
            {
                list.AddRange(userIds);
                return list;
            });
        }

        _logger.LogInformation("Loaded {Count} blocked users", _blockedUsers.Sum(kvp => kvp.Value.Count));
    }
}
