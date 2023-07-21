using DSharpPlus.Entities;
using SuggestionBot.Configuration;

namespace SuggestionBot.Services;

internal sealed class CooldownService
{
    private readonly ConfigurationService _configurationService;
    private readonly SuggestionService _suggestionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CooldownService" /> class.
    /// </summary>
    /// <param name="configurationService">The <see cref="ConfigurationService" />.</param>
    /// <param name="suggestionService">The <see cref="SuggestionService" />.</param>
    public CooldownService(ConfigurationService configurationService, SuggestionService suggestionService)
    {
        _configurationService = configurationService;
        _suggestionService = suggestionService;
    }

    /// <summary>
    ///     Gets the remaining cooldown for the specified member.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <returns>The remaining cooldown.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="member" /> is <see langword="null" />.</exception>
    public TimeSpan GetRemainingCooldown(DiscordMember member)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        if (!_configurationService.TryGetGuildConfiguration(member.Guild, out GuildConfiguration? configuration))
        {
            configuration = new GuildConfiguration();
        }

        if (configuration.Cooldown <= 0)
        {
            return TimeSpan.Zero;
        }

        ulong[] roleIds = member.Roles.Select(r => r.Id).ToArray();
        ulong[] exemptRoleIds = configuration.CooldownExemptRoles;

        if (roleIds.Length > 0 && exemptRoleIds.Length > 0 && exemptRoleIds.Any(roleIds.Contains))
        {
            return TimeSpan.Zero;
        }

        DateTimeOffset lastSuggestionTime = _suggestionService.GetLastSuggestionTime(member);
        return lastSuggestionTime + TimeSpan.FromSeconds(configuration.Cooldown) - DateTimeOffset.UtcNow;
    }
}
