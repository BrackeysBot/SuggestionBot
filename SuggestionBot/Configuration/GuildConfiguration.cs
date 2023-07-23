namespace SuggestionBot.Configuration;

/// <summary>
///     Represents the configuration for a guild.
/// </summary>
public sealed class GuildConfiguration
{
    /// <summary>
    ///     Gets or sets the embed color for accepted suggestions.
    /// </summary>
    /// <value>The embed color for accepted suggestions.</value>
    public int AcceptedColor { get; set; } = 0x00FF00;

    /// <summary>
    ///     Gets or sets the cooldown for posting suggestions.
    /// </summary>
    /// <value>The cooldown for posting suggestions.</value>
    public int Cooldown { get; set; } = 3600;

    /// <summary>
    ///     Gets or sets the exempt roles for posting suggestions.
    /// </summary>
    /// <value>The exempt roles for posting suggestions.</value>
    public ulong[] CooldownExemptRoles { get; set; } = Array.Empty<ulong>();

    /// <summary>
    ///     Gets or sets the embed color for implemented suggestions.
    /// </summary>
    /// <value>The embed color for implemented suggestions.</value>
    public int ImplementedColor { get; set; } = 0x191970;

    /// <summary>
    ///     Gets or sets the log channel ID.
    /// </summary>
    /// <value>The log channel ID.</value>
    public ulong LogChannel { get; set; }

    /// <summary>
    ///     Gets or sets the embed color for rejected suggestions.
    /// </summary>
    /// <value>The embed color for rejected suggestions.</value>
    public int RejectedColor { get; set; } = 0xFF0000;

    /// <summary>
    ///     Gets or sets the channel ID for posting suggestions.
    /// </summary>
    /// <value>The channel ID for posting suggestions.</value>
    public ulong SuggestionChannel { get; set; }

    /// <summary>
    ///     Gets or sets the embed color for new suggestions.
    /// </summary>
    /// <value>The embed color for new suggestions.</value>
    public int SuggestedColor { get; set; } = 0xFFFF00;
}
