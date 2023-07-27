# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.6.3] - 2023-07-27

### Added

- Added repo URL to `/info` embed.
- #channel_name in suggestion content is now automatically converted to a mention string.

## [1.6.2] - 2023-07-26

### Added

- Added status which explains bot usage.

## [1.6.1] - 2023-07-24

### Added

- Added additional suggestion states.

## [1.6.0] - 2023-07-24

### Added

- Added optional staff remarks to suggestion embeds.
- Staff can now remove suggestions.

### Changed

- `/suggestion view` now uses the same search logic as `/suggestion setstatus`.
- Suggestion updates are now sent to the author. 

## [1.5.1] - 2023-07-24

### Added

- The user who posted the suggestion is now automatically added to the suggestion thread.

## [1.5.0] - 2023-07-24

### Added

- Suggestions now spawn a thread when they are posted. These threads are closed when the suggestion is marked as
implemented, accepted, or rejected.
- Suggestion status update embed now shows link to suggestion.
- Added autocomplete to `/suggestion view`.

### Fixed
- Fixed autocomplete for `/suggestion setstatus`.

## [1.4.0] - 2023-07-23

### Added

- Add new ACCEPTED suggestion status.

### Changed

- `/suggestion implement` and `/suggestion reject` combined to `/suggestion setstatus`.

## [1.3.2] - 2023-07-22

### Added

- Add suggestion link to staff embed.

## [1.3.1] - 2023-07-22

### Added

- Staff-specific embed for viewing suggestions.

## [1.3.0] - 2023-07-22

### Added

- Suggestion embeds now include the timestamp.

### Changed

- Improved performance of pretty much the entire codebase.
- Initial suggestion embed no longer marked as "edited".

## [1.2.1] - 2023-07-21

### Fixed

- Fixed an issue where users could not use `/suggest` command.

## [1.2.0] - 2023-07-21

### Added

- Added `/suggestion view` command.

## [1.1.0] - 2023-07-21

### Added

- Added cooldown exemption roles.

## [1.0.2] - 2023-07-21

### Fixed

- Fixed a bug where not all code paths for `/suggest` were ephemeral.

## [1.0.1] - 2023-07-21

### Changed

- `/suggest` command is now ephemeral.

## [1.0.0] - 2023-07-21

### Added

- Initial release.

[1.6.3]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.6.3
[1.6.2]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.6.2
[1.6.1]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.6.1
[1.6.0]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.6.0
[1.5.1]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.5.1
[1.5.0]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.5.0
[1.4.0]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.4.0
[1.3.2]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.3.2
[1.3.1]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.3.1
[1.3.0]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.3.0
[1.2.1]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.2.1
[1.2.0]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.2.0
[1.1.0]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.1.0
[1.0.2]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.0.2
[1.0.1]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.0.1
[1.0.0]: https://github.com/BrackeysBot/SuggestionBot/releases/tag/v1.0.0
