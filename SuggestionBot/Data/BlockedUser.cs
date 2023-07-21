namespace SuggestionBot.Data;

internal sealed class BlockedUser
{
    /// <summary>
    ///     Gets or sets the ID of the guild where the user is blocked.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the reason for blocking the user.
    /// </summary>
    /// <value>The reason for blocking the user.</value>
    public string? Reason { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the staff member who blocked the user.
    /// </summary>
    /// <value>The ID of the staff member who blocked the user.</value>
    public ulong StaffMemberId { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user was blocked.
    /// </summary>
    /// <value>The timestamp when the user was blocked.</value>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the blocked user.
    /// </summary>
    /// <value>The ID of the blocked user.</value>
    public ulong UserId { get; set; }
}
