# ProFootball

![Build](https://img.shields.io/badge/build-local-informational)
![Coverage](https://img.shields.io/badge/coverage-70%25%20target-blue)

ProFootball is a .NET 10 WPF monorepo with Clean Architecture boundaries (`Domain`, `Application`, `Infrastructure`, `Presentation`) and a PostgreSQL-backed read-only MVP for football analytics.

## Quickstart

### Prerequisites
- .NET SDK `10.0.104` (pinned in `global.json`)
- PostgreSQL 15+ (local or remote)
- PowerShell 7+ (for helper scripts)

### Install
```powershell
dotnet restore ProFootball.slnx
```

### Development Run
```powershell
.\scripts\tasks.ps1 dev
```

### Import Dataset (SQLite -> PostgreSQL)
```powershell
dotnet run --project .\src\ProFootball.DataLoader\ProFootball.DataLoader.csproj -- --sqlite "C:\path\to\database.sqlite"
```
`DataLoader` resolves `country_id` from SQLite into text `CountryName` fields used by the final model.

With synthetic analytics generation (default profile/mode):
```powershell
dotnet run --project .\src\ProFootball.DataLoader\ProFootball.DataLoader.csproj -- --sqlite "C:\path\to\database.sqlite" --batch-size 1000 --profile realistic --mode regenerate
```

Supported options:
- `--profile lite|realistic|stress` (`realistic` by default)
- `--mode regenerate|append` (`regenerate` by default)
- `--batch-size <positive-int>` (`1000` by default)

### Test
```powershell
.\scripts\tasks.ps1 test
```

### Build
```powershell
.\scripts\tasks.ps1 build
```

## Configuration

Create local `.env` from `.env.example` and set values as needed.

| Variable | Required | Default | Description |
|---|---|---|---|
| `PROFOOTBALL_DB_CONNECTION_STRING` | No* | `Host=localhost;Port=5432;Database=profootball;Username=postgres;Password=postgres` | PostgreSQL connection string fallback. |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` | Environment name for configuration behavior. |

`*` Required if `ConnectionStrings:ProFootballDb` is absent from `appsettings.json`.

## Logging

- Logs are written to `logs/` next to the executable (`AppContext.BaseDirectory`).
- File name pattern: `profootball-.log` (daily rolling, retain last 14 files).
- `Debug` build logs: `Debug` and above.
- `Release` build logs: `Warning` and above.
- Log records include timestamp, level, source context, message, and exception stacktrace.

## UI Theme System

- Toolkit: `ModernWpfUI` with shared token dictionaries in `src/ProFootball.Presentation/Themes`.
- Runtime theme toggle is available in the header (`Light` / `Dark`) and persists per-user in:
  - `%LOCALAPPDATA%\ProFootball\theme.settings.json`
- Core tokens:
  - Light: `Background #f8f9fa`, `Card #ffffff`, `Border #dee2e6`, `Text #1a1d1f`, `Muted #6c757d`
  - Dark: `Background #0f1214`, `Card #1a1d1f`, `Border #2d3236`, `Text #f8f9fa`, `Muted #9ca3af`
  - Primary accent: `#22c55e`

## App Icon (Title Bar + Taskbar)

To use your own icon in both the window frame and Windows taskbar:

1. Add an `.ico` file to `src/ProFootball.Presentation/Assets/` (for example `app.ico`).
2. Set executable icon in `src/ProFootball.Presentation/ProFootball.Presentation.csproj`:
```xml
<PropertyGroup>
  <ApplicationIcon>Assets\app.ico</ApplicationIcon>
</PropertyGroup>
```
3. Set window icon in `src/ProFootball.Presentation/MainWindow.xaml`:
```xml
<Window ... Icon="Assets/app.ico">
```

`ApplicationIcon` controls the executable/taskbar icon, and `Icon` ensures the same icon is shown in the window frame.

## Commands Cheat Sheet

```powershell
.\scripts\tasks.ps1 dev
.\scripts\tasks.ps1 build
.\scripts\tasks.ps1 test
.\scripts\tasks.ps1 lint
.\scripts\tasks.ps1 format
.\scripts\tasks.ps1 clean
dotnet run --project .\src\ProFootball.DataLoader\ProFootball.DataLoader.csproj -- --sqlite "<path>" --batch-size 1000
dotnet run --project .\src\ProFootball.DataLoader\ProFootball.DataLoader.csproj -- --sqlite "<path>" --batch-size 1000 --profile realistic --mode regenerate
dotnet run --project .\src\ProFootball.DataLoader\ProFootball.DataLoader.csproj -- --sqlite "<path>" --profile stress --mode append
```

## Project Structure

- `src/ProFootball.Domain`: domain entities.
- `src/ProFootball.Application`: query/import contracts and read-model abstractions.
- `src/ProFootball.Infrastructure`: EF Core + PostgreSQL persistence, migrations, query services, and SQLite import service.
- `src/ProFootball.Presentation`: WPF MVVM client with dashboard, list/detail pages, and analytics views.
- `src/ProFootball.DataLoader`: console data-loader for importing SQLite dataset into PostgreSQL.
- `tests/ProFootball.Domain.Tests`: xUnit domain tests (entities, invariants, validation).
- `tests/ProFootball.Application.Tests`: xUnit application tests (auth/session behavior).
- `tests/ProFootball.Infrastructure.Tests`: xUnit infrastructure tests (importing, querying, helpers).
- `docs/adr`: architecture decision records.

## Architecture Overview

The solution follows a layered architecture where core business concepts stay in `Domain`, use-case-level contracts and lightweight behavior sit in `Application`, and technical integrations are isolated in `Infrastructure`.

`Presentation` bootstraps the desktop app, composes dependencies, and interacts with application-facing abstractions. This keeps UI concerns separate from data access and makes future refactors (e.g., alternate clients or auth providers) predictable.

## Decisions

- ADR template: `docs/adr/ADR-000-template.md`
- Initial architecture/tooling decision: `docs/adr/ADR-001-architecture-and-tooling.md`

## Contributing and Code of Conduct

- Contributing guide: `CONTRIBUTING.md`
- Code of Conduct: `CODE_OF_CONDUCT.md`

## Quality and Coverage

- Formatting/linting: `dotnet format`
- Tests: `xUnit`
- Coverage threshold: `70%` line coverage for core test projects (`Directory.Build.targets`)
- Pre-commit hook: `.githooks/pre-commit` (enable with `git config core.hooksPath .githooks`)

## MVP Scope

1. Entities: `Country`, `League`, `Team`, `TeamAttribute`, `Player`, `PlayerAttribute`, `FootballMatch`.
2. UI pages: `Dashboard`, `Countries/Leagues`, `Teams`, `Team Details`, `Players`, `Player Details`, `Matches`, `Match Details`, `Analytics`.
3. Analytics: player rating/potential trend, top players by filters, matches by season/league.
