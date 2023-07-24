namespace SuggestionBot.Data;

/// <summary>
///     Represents a suggestion.
/// </summary>
public sealed class Suggestion
{
    /// <summary>
    ///     Gets or sets the ID of the author of the suggestion.
    /// </summary>
    /// <value>The author ID.</value>
    public ulong AuthorId { get; set; }

    /// <summary>
    ///     Gets or sets the content of the suggestion.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the ID of the guild in which the suggestion was made.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the suggestion.
    /// </summary>
    /// <value>The suggestion ID.</value>
    public long Id { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the message that represents the suggestion.
    /// </summary>
    /// <value>The message ID.</value>
    public ulong MessageId { get; set; }

    /// <summary>
    ///     Gets or sets the additional remarks about the suggestion.
    /// </summary>
    /// <value>The remarks.</value>
    public string? Remarks { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the staff member who implemented or rejected the suggestion.
    /// </summary>
    /// <value>The staff member ID.</value>
    public ulong? StaffMemberId { get; set; }

    /// <summary>
    ///     Gets or sets the status of the suggestion.
    /// </summary>
    /// <value>The suggestion status.</value>
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Suggested;

    /// <summary>
    ///     Gets or sets the ID of the suggestion's discussion thread.
    /// </summary>
    /// <value>The thread ID.</value>
    public ulong ThreadId { get; set; }

    /// <summary>
    ///     Gets or sets the date and time at which the suggestion was made.
    /// </summary>
    /// <value>The suggestion timestamp.</value>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
