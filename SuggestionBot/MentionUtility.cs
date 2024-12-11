using DSharpPlus.Entities;

namespace SuggestionBot;

internal sealed class MentionUtility
{
    /// <summary>
    ///     Returns a channel mention string built from the specified channel ID.
    /// </summary>
    /// <param name="id">The ID of the channel to mention.</param>
    /// <returns>A channel mention string in the format <c>&lt;#123&gt;</c>.</returns>
    public static string MentionChannel(ulong id)
    {
        return $"<#{id}>";
    }

    /// <summary>
    ///     Returns a user mention string built from the specified user ID.
    /// </summary>
    /// <param name="id">The ID of the user to mention.</param>
    /// <returns>A user mention string in the format <c>&lt;@123&gt;</c>.</returns>
    public static string MentionUser(ulong id)
    {
        return MentionUser(id, false);
    }

    /// <summary>
    ///     Returns a user mention string built from the specified user ID.
    /// </summary>
    /// <param name="id">The ID of the user to mention.</param>
    /// <param name="nickname">
    ///     <see langword="true" /> if the mention string should account for nicknames; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>
    ///     A user mention string in the format <c>&lt;@!123&gt;</c> if <paramref name="nickname" /> is <see langword="true" />,
    ///     or in the format <c>&lt;@123&gt;</c> if <paramref name="nickname" /> is <see langword="false" />.
    /// </returns>
    public static string MentionUser(ulong id, bool nickname)
    {
        return nickname ? $"<@!{id}>" : $"<@{id}>";
    }

    /// <summary>
    ///     Replaces raw channel mentions with Discord channel mentions.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <param name="input">The input to sanitize.</param>
    /// <returns>The sanitized input.</returns>
    public static string ReplaceChannelMentions(DiscordGuild guild, string input)
    {
        foreach (DiscordChannel channel in guild.Channels.Values)
        {
            input = input.Replace($"#{channel.Name}", channel.Mention);
        }

        return input;
    }
}
