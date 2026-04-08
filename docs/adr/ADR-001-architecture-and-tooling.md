# ADR-001: Initial Architecture and Tooling

- Status: Accepted
- Date: 2026-04-04

## Context
The project is a WPF app monorepo with Domain, Application, Infrastructure, and Presentation projects.
We need an immediately runnable baseline with quality checks, tests, and PostgreSQL integration hooks.

## Decision
Adopt Clean Architecture layering:
- `Domain`: entities and pure business rules.
- `Application`: abstractions and in-memory session placeholder service.
- `Infrastructure`: EF Core + Npgsql persistence and repository implementations.
- `Presentation`: WPF host startup, dependency injection, and UI.

Adopt centralized repository standards:
- Root-level `Directory.Build.props/targets` and central package versions.
- Quality commands via `scripts/tasks.ps1`.
- Git pre-commit hook running format, build, and tests.

## Consequences
- Faster onboarding and predictable local workflow.
- Clear extension points for auth and persistence.
- Additional setup required for Git hooks (`git config core.hooksPath .githooks`).
