﻿using System.Diagnostics.CodeAnalysis;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using SuggestionBot.Configuration;

namespace SuggestionBot.Services;

/// <summary>
///     Represents a service which can read configuration values from a file.
/// </summary>
internal sealed class ConfigurationService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfigurationService" /> class.
    /// </summary>
    /// <param name="configuration">The app configuration.</param>
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    ///     Gets the bot configuration for the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose configuration to retrieve.</param>
    /// <returns>
    ///     A <see cref="GuildConfiguration" /> containing the configuration, or <see langword="null" /> if no configuration is
    ///     defined.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public GuildConfiguration? GetGuildConfiguration(DiscordGuild guild)
    {
        if (guild is null)
        {
            throw new ArgumentNullException(nameof(guild));
        }

        return _configuration.GetSection(guild.Id.ToString())?.Get<GuildConfiguration>();
    }

    /// <summary>
    ///     Gets the bot configuration for the specified guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild whose configuration to retrieve.</param>
    /// <returns>
    ///     A <see cref="GuildConfiguration" /> containing the configuration, or <see langword="null" /> if no configuration is
    ///     defined.
    /// </returns>
    public GuildConfiguration? GetGuildConfiguration(ulong guildId)
    {
        return _configuration.GetSection(guildId.ToString()).Get<GuildConfiguration>();
    }

    /// <summary>
    ///     Attempts to get the bot configuration for the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose configuration to retrieve.</param>
    /// <param name="configuration">
    ///     When this method returns, contains the <see cref="GuildConfiguration" /> for the specified guild, or
    ///     <see langword="null" /> if no configuration is defined.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> if the specified guild has a configuration; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetGuildConfiguration(DiscordGuild guild, [NotNullWhen(true)] out GuildConfiguration? configuration)
    {
        configuration = null;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return guild is not null && TryGetGuildConfiguration(guild.Id, out configuration);
    }

    /// <summary>
    ///     Attempts to get the bot configuration for the specified guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild whose configuration to retrieve.</param>
    /// <param name="configuration">
    ///     When this method returns, contains the <see cref="GuildConfiguration" /> for the specified guild, or
    ///     <see langword="null" /> if no configuration is defined.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> if the specified guild has a configuration; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetGuildConfiguration(ulong guildId, [NotNullWhen(true)] out GuildConfiguration? configuration)
    {
        configuration = GetGuildConfiguration(guildId);
        return configuration is not null;
    }
}
