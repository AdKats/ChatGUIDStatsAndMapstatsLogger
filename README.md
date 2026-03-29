# PRoCon Chat, GUID, Stats and Map Logger

A PRoCon Frostbite plugin that logs player chat, GUIDs, player statistics, weapon stats, and map stats to a MySQL database.

## Features

- **Chat Logging** - Log all player chat (global, team, squad) with optional regex filtering
- **Player Statistics** - Track kills, deaths, headshots, score, playtime, K/D ratio, killstreaks
- **Weapon Statistics** - Per-weapon kill/death/headshot tracking
- **Map Statistics** - Round duration, player counts, join/leave tracking
- **Session Tracking** - Per-session player stats with database persistence
- **Dogtag Tracking** - Knife kill victim/killer relationships
- **In-Game Commands** - Players can query their stats, top 10, weapon stats, session data
- **Welcome Stats** - Greet returning players with their server stats
- **Multi-Game Support** - BF3, BF4, BFBC2, MOH, MOHW

## Requirements

- MySQL 5.1+ with InnoDB engine support
- PRoCon with database outgoing connections enabled

## Installation

1. Copy all `.cs` files from `src/` to your PRoCon plugins directory
2. Configure database connection in plugin settings
3. Enable the plugin - tables are created automatically

## Configuration

Configure via PRoCon plugin settings:
- **Server Details** - MySQL host, port, database, credentials, connection pooling
- **Chatlogging** - Enable/disable, server message filtering, instant logging
- **Stats** - Enable/disable stats, weapons, rankings, in-game commands
- **WelcomeStats** - Greeting messages for returning/new players
- **MapStats** - Round statistics tracking
- **Session** - Session tracking and database persistence

## In-Game Commands

| Command | Description |
|---------|-------------|
| `@stats` / `@rank` | Show your server stats |
| `@top10` | Show top 10 players |
| `@stats <weapon>` | Show your weapon stats |
| `@session` | Show current session data |
| `@serverstats` | Show server statistics |
| `@potd` | Show player of the day |
| `@dogtags` | Show dogtag stats |
| `@wtop10` | Show top 10 for period |

## License

GPLv3 - See [LICENSE](LICENSE) for details.

## Author

Original author: [GWC]XpKiller
