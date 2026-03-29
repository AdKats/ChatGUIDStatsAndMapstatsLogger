# CLAUDE.md - CChatGUIDStatsLogger

## Project overview
PRoCon Frostbite plugin that logs player chat, GUIDs, statistics, weapon stats, and map stats to a MySQL database. Supports BF3, BF4, BFBC2, MOH, MOHW.

## Architecture
- **Partial class layout**: `CChatGUIDStatsLogger` is split across multiple files in `src/`
- **Namespace**: `PRoConEvents`
- **Base class**: `PRoConPluginAPI` implementing `IPRoConPluginInterface`
- **Database**: MySQL via MySqlConnector (migrated from MySql.Data.MySqlClient)

### Source files
| File | Purpose |
|------|---------|
| `src/CChatGUIDStatsLogger.cs` | Main entry: metadata, lifecycle, constructor, state, helper classes |
| `src/CChatGUIDStatsLogger/Settings.cs` | Plugin variable get/set (Procon settings UI) |
| `src/CChatGUIDStatsLogger/Events.cs` | Procon event handlers (OnPlayerJoin, OnPlayerKilled, etc.) |
| `src/CChatGUIDStatsLogger/Commands.cs` | External commands (AdKats interop) + in-game commands |
| `src/CChatGUIDStatsLogger/Database.cs` | MySQL connection, table creation, queries, ranking |
| `src/CChatGUIDStatsLogger/PlayerStats.cs` | Player stats tracking, streaming, sessions, weapons |
| `src/CChatGUIDStatsLogger/ChatLog.cs` | Chat logging to database |

## Database schema
The plugin auto-creates the following InnoDB tables (with optional `tableSuffix`):

| Table | Purpose |
|-------|---------|
| `tbl_games` | Game type registry (BF3, BF4, etc.) |
| `tbl_playerdata` | Player identity (EAGUID, PBGUID, IP, country) |
| `tbl_server` | Server registry (IP, name, game, group) |
| `tbl_server_player` | Player-server association (StatsID) |
| `tbl_playerstats` | Per-server player stats (score, kills, deaths, etc.) |
| `tbl_playerrank` | Cross-server ranking |
| `tbl_weapons` | Weapon definitions per game |
| `tbl_weapons_stats` | Per-weapon player stats |
| `tbl_dogtags` | Knife kill tracking (killer/victim) |
| `tbl_chatlog` | Chat message log |
| `tbl_mapstats` | Round/map statistics |
| `tbl_sessions` | Player session tracking |
| `tbl_currentplayers` | Live scoreboard snapshot |
| `tbl_server_stats` | Aggregated server statistics |
| `tbl_teamscores` | Team score/ticket tracking |

## Build and format
This project uses a .csproj for `dotnet format` / CI style checks only. PRoCon v2 assemblies are not available for compilation.

```bash
# Restore NuGet packages
dotnet restore

# Format whitespace
dotnet format whitespace

# Format code style
dotnet format style --severity warn --exclude-diagnostics IDE1007
```

## Code style
- System types preferred: `String`, `Int32`, `Boolean` (not `string`, `int`, `bool`)
- Allman brace style
- `using` directives outside namespace
- Block-scoped namespaces

## Author
Original author: [GWC]XpKiller (www.german-wildcards.de)
License: GPLv3
