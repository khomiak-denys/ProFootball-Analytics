# ProFootball

![Build](https://img.shields.io/badge/build-local-informational)
![Coverage](https://img.shields.io/badge/coverage-70%25%20target-blue)

ProFootball is a .NET 10 WPF monorepo with Clean Architecture boundaries (`Domain`, `Application`, `Infrastructure`, `Presentation`) and a PostgreSQL baseline.

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

## Commands Cheat Sheet

```powershell
.\scripts\tasks.ps1 dev
.\scripts\tasks.ps1 build
.\scripts\tasks.ps1 test
.\scripts\tasks.ps1 lint
.\scripts\tasks.ps1 format
.\scripts\tasks.ps1 clean
```

## Project Structure

- `src/ProFootball.Domain`: domain entities.
- `src/ProFootball.Application`: application contracts and session placeholder service.
- `src/ProFootball.Infrastructure`: EF Core + PostgreSQL persistence and repository implementations.
- `src/ProFootball.Presentation`: WPF entrypoint, DI composition, and UI.
- `tests/ProFootball.Application.Tests`: xUnit tests with coverage threshold.
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
- Coverage threshold: `70%` line coverage (`Directory.Build.targets`)
- Pre-commit hook: `.githooks/pre-commit` (enable with `git config core.hooksPath .githooks`)

## Next Steps

1. Add first EF Core migration (`src/ProFootball.Infrastructure/Migrations/README.md`).
2. Replace in-memory session placeholder with real authentication flow.
3. Expand domain use-cases and UI navigation.
