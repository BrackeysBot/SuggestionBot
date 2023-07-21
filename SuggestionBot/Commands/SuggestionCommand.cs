using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using SuggestionBot.Services;

namespace SuggestionBot.Commands;

[SlashCommandGroup("suggestion", "Manage suggestions.", false)]
[SlashRequireGuild]
internal sealed partial class SuggestionCommand : ApplicationCommandModule
{
    private readonly SuggestionService _suggestionService;
    private readonly UserBlockingService _userBlockingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SuggestionCommand" /> class.
    /// </summary>
    /// <param name="suggestionService">The <see cref="SuggestionService" />.</param>
    /// <param name="userBlockingService">The <see cref="UserBlockingService" />.</param>
    public SuggestionCommand(SuggestionService suggestionService, UserBlockingService userBlockingService)
    {
        _suggestionService = suggestionService;
        _userBlockingService = userBlockingService;
    }
}
