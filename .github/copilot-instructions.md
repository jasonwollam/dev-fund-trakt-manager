# Copilot Instructions

## Quick orientation
- Start with `.github/instructions/architecture.instructions.md` for the full clean-architecture map (projects, dependencies, tests).
- `.github/instructions/trakt-api.instructions.md` mirrors `spec/trakt.apib` and is the canonical contract for infrastructure code hitting Trakt.
- `.github/instructions/trakt-slice-catalog.md` documents vertical slices of Trakt functionality and their implementation status.

## Solution layout (see architecture doc for detail)
- `src/DevFund.TraktManager.Domain` — entities/value objects (`Show`, `Episode`, `CalendarEntry`, `TraktIds`).
- `src/DevFund.TraktManager.Application` — use cases (`CalendarService`, `CalendarOrchestrator`), contracts, and ports (`ITraktCalendarClient`, `ICalendarPresenter`).
- `src/DevFund.TraktManager.Infrastructure` — HTTP implementers (`TraktCalendarClient`), DTOs, and DI wiring aligned with the Trakt API.
- `src/DevFund.TraktManager.Presentation.Cli` — console host/bootstrap plus `ConsoleCalendarPresenter` as the default presenter.
- Matching `tests/*` projects ensure every layer has coverage; keep parity when adding artifacts.

## Core workflows
- Restore/build/test from the repo root:
  ```bash
  dotnet restore
  dotnet build
  dotnet test
  ```
- Run the CLI presenter after configuring `src/DevFund.TraktManager.Presentation.Cli/appsettings.json` with real Trakt credentials:
  ```bash
  dotnet run --project src/DevFund.TraktManager.Presentation.Cli -- --start=2024-01-01 --days=7
  ```
- New packages go through `dotnet add <project> package <name>`; never edit `*.csproj` manually when a CLI command exists.

## Conventions & patterns
- Respect dependency flow (Presentation → Infrastructure → Application → Domain). Never reference outward.
- Register services via the provided extension methods: `AddApplicationLayer` and `AddInfrastructureLayer`.
- Infrastructure adapters must read headers/endpoints from the Trakt instructions file; document new endpoints there when added.
- Prefer constructor injection, cancellation tokens, and async flows. Avoid static state.

## When expanding features
- Choose a slice from the catalog before diving in so you cover every layer and test set intentionally.
- Start with Domain abstractions, follow with Application ports/contracts, then implement Infrastructure or additional Presenters.
- Add/extend the sibling test project the moment you create a new class in any layer.
- Update this document plus the architecture instructions whenever the structure or workflows change so downstream agents stay in sync.
