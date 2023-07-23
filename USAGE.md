# Slash Commands

Below is an outline of every slash command currently implemented in SuggestionBot, along with their descriptions and
parameters.

The primary command used by the public is `/suggest`.

### `/suggest`

Allows a user to submit a suggestion. This presents the user with a modal to fill out the suggestion details.

| Parameter | Required | Type | Description |
|:----------|:---------|:-----|:------------|
| -         | -        | -    | -           |

## User Blocking

Users have the ability to post suggestions. However, this opens up the potential to be abused. If a user is
sending too many frivolous suggestions, their suggestions can be blocked so that their suggestions are no longer
acknowledged.

### `/suggestion block`

Prevent a user from sending suggestions.

| Parameter | Required | Type               | Description                          |
|:----------|:---------|:-------------------|:-------------------------------------|
| user      | ✅ Yes    | User mention or ID | The user whose suggestions to block. |
| reason    | ❌ No     | String             | The reason for the block.            |

### `/suggestion unblock`

Allow a user to send suggestions again.

| Parameter | Required | Type               | Description                            |
|:----------|:---------|:-------------------|:---------------------------------------|
| user      | ✅ Yes    | User mention or ID | The user whose suggestions to unblock. |

## Implementing and Rejecting Suggestions

### `/suggestion setstatus`

Change the status of a suggestion

| Parameter  | Required | Type                     | Description                            |
|:-----------|:---------|:-------------------------|:---------------------------------------|
| suggestion | ✅ Yes    | Suggestion or Message ID | The suggestion whose status to change. |
| status     | ✅ Yes    | SuggestionStatus         | The new status of the suggestion.      |

# Ephemeral responses

Below is a table outlining all the commands and whether or not they have ephemeral responses.

| Command                 | Ephemeral Response |
|:------------------------|:-------------------|
| `/suggest`              | ✅ Yes              |
| `/suggestion implement` | ❌ No               |
| `/suggestion reject`    | ❌ No               |
| `/suggestion block`     | ✅ Yes              |
| `/suggestion unblock`   | ✅ Yes              |
