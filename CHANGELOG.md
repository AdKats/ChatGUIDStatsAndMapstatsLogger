# Changelog

## Unreleased

### Changed
- Converted from `.inc` + game-variant stubs to partial class layout in `src/`
- Split monolithic file into partial classes: Settings, Events, Commands, Database, PlayerStats, ChatLog
- Removed `Plugins/` directory with game-variant stub files (BF3, BF4, BFBC2, MOH, MOHW)
- Migrated `MySql.Data.MySqlClient` to `MySqlConnector`
- Added `.editorconfig` for consistent code style
- Added `CChatGUIDStatsLogger.csproj` for CI format checks
- Added CI and release GitHub Actions workflows
- Added `CLAUDE.md` with architecture and database schema documentation

## 1.0.0.4

- Allow NON PB enabled Servers to use the Plugin for Stats tracking

## 1.0.0.2

- Bugfixes for column errors
- Bugfixes for the sessions streaming bug
- Weaponstats working again
- Bugfix for Identifier name is too long

## 1.0.0.1

- Bugfixes for value too long for column errors
- Bugfixes for some other bugs
- Changed deprecated Tracemessages
- Added an error prefix in pluginlog
- New feature: Tickets/teamscores are now tracked in tbl_teamscores
- New feature: Simple Stats (collects playerdata only)
- New feature: Switch for disabling weaponstats

## 1.0.0.0

- First Release
- Multigame Support
