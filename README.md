<h1 align="center">SuggestionBot</h1>
<p align="center"><img src="icon.png" width="128"></p>
<p align="center"><i>A Discord bot for suggestions.</i></p>
<p align="center">
<a href="https://github.com/BrackeysBot/SuggestionBot/releases"><img src="https://img.shields.io/github/v/release/BrackeysBot/SuggestionBot?include_prereleases&style=flat-square"></a>
<a href="https://github.com/BrackeysBot/SuggestionBot/actions/workflows/dotnet.yml"><img src="https://img.shields.io/github/actions/workflow/status/BrackeysBot/SuggestionBot/dotnet.yml?branch=main&style=flat-square" alt="GitHub Workflow Status" title="GitHub Workflow Status"></a>
<a href="https://github.com/BrackeysBot/SuggestionBot/issues"><img src="https://img.shields.io/github/issues/BrackeysBot/SuggestionBot?style=flat-square" alt="GitHub Issues" title="GitHub Issues"></a>
<a href="https://github.com/BrackeysBot/SuggestionBot/blob/main/LICENSE.md"><img src="https://img.shields.io/github/license/BrackeysBot/SuggestionBot?style=flat-square" alt="MIT License" title="MIT License"></a>
</p>

## About
SuggestionBot is a Discord bot which allows users to submit suggestions.

## Installing and configuring SuggestionBot 
SuggestionBot runs in a Docker container, and there is a [docker-compose.yml](docker-compose.yml) file which simplifies this process.

### Clone the repository
To start off, clone the repository into your desired directory:
```bash
git clone https://github.com/BrackeysBot/SuggestionBot.git
```
Step into the SuggestionBot directory using `cd SuggestionBot`, and continue with the steps below.

### Setting things up
The bot's token is passed to the container using the `DISCORD_TOKEN` environment variable. Create a file named `.env`, and add the following line:
```
DISCORD_TOKEN=your_token_here
```

Two directories are required to exist for Docker compose to mount as container volumes, `data` and `logs`:
```bash
mkdir data
mkdir logs
```
Copy the example `config.example.json` to `data/config.json`, and assign the necessary config keys. Below is breakdown of the config.json layout:
```json
{
  "GUILD_ID": {
    "logChannel": /* The ID of the log channel */,
    "suggestionChannel": /* The ID of the channel in which suggestions are posted */,
    "suggestedColor": /* The default color for suggestions, as a 24-bit RGB integer. Defaults to #FFFF00 */,
    "implementedColor": /* The color for implemented suggestions, as a 24-bit RGB integer. Defaults to #191970 */,
    "rejectedColor": /* The color for rejected suggestions, as a 24-bit RGB integer. Defaults to #FF0000 */,
    "cooldown": /* The cooldown between suggestion posting. Defaults to 3600 */
  }
}
```
The `logs` directory is used to store logs in a format similar to that of a Minecraft server. `latest.log` will contain the log for the current day and current execution. All past logs are archived.

The `data` directory is used to store persistent state of the bot, such as config values and the infraction database.

### Launch SuggestionBot
To launch SuggestionBot, simply run the following commands:
```bash
sudo docker-compose build
sudo docker-compose up --detach
```

## Updating SuggestionBot
To update SuggestionBot, simply pull the latest changes from the repo and restart the container:
```bash
git pull
sudo docker-compose stop
sudo docker-compose build
sudo docker-compose up --detach
```

## Using SuggestionBot
For further usage breakdown and explanation of commands, see [USAGE.md](USAGE.md).

## License
This bot is under the [MIT License](LICENSE.md).

## Disclaimer
This bot is tailored for use within the [Brackeys Discord server](https://discord.gg/brackeys). While this bot is open source and you are free to use it in your own servers, you accept responsibility for any mishaps which may arise from the use of this software. Use at your own risk.
