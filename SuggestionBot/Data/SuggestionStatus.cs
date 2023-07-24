namespace SuggestionBot.Data;

/// <summary>
///     An enumeration of suggestion statuses.
/// </summary>
public enum SuggestionStatus
{
    Suggested,
    Rejected,
    Implemented,
    Accepted,
    Removed,
    Duplicate,
    AlreadyImplemented,
    AlreadyPlanned
}
