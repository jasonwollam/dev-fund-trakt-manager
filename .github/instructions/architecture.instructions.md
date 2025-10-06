---
applyTo: '**'
---
# Clean architecture map (start here)
- This document is the authoritative guide to the solution structure. Consult it whenever you add features, wire dependencies, or review cross-layer changes, and keep it up to date.

## Solution layout
- `src/DevFund.TraktManager.Domain` — Pure domain model: entities (`Show`, `Episode`, `CalendarEntry`) and value objects (`TraktIds`). No external dependencies.
- `src/DevFund.TraktManager.Application` — Use cases and abstractions: contracts (`CalendarRequest`), service orchestration (`CalendarService`, `CalendarOrchestrator`), and ports (`ITraktCalendarClient`, `ICalendarPresenter`). Depends only on Domain.
- `src/DevFund.TraktManager.Infrastructure` — Implementer layer: HTTP clients, DTOs, and DI extensions that satisfy application ports using the Trakt spec (`spec/trakt.apib`). Depends on Application and external packages.
- `src/DevFund.TraktManager.Presentation.Cli` — Presenter boundary: console host/bootstrap plus `ConsoleCalendarPresenter` for rendering results. Depends on Application for orchestration and Infrastructure for concrete adapters.

## Dependency rules
- Flow inwards only: Presentation → Infrastructure → Application → Domain.
- Domain never references other projects; keep new concepts here first.
- Application exposes abstractions for infrastructure/presenter implementations; add new interfaces here when introducing additional persistence providers or front-ends.

## Authentication & authorization guardrails
- All authentication/authorization workflows (device code polling, token storage, refresh logic) live in the infrastructure project. The application layer describes the ports (`IDeviceAuthenticationService`, `ITraktDeviceAuthClient`, `ITraktAccessTokenStore`) but never owns behaviour.
- Presentation layers resolve `IDeviceAuthenticationService` from DI; do not instantiate infrastructure types directly or reimplement the device flow outside the infrastructure boundary.
- When integrating new auth providers or stores, add the interface to `Application.Abstractions`, implement it under `Infrastructure`, and register it in `InfrastructureServiceCollectionExtensions` alongside the existing Trakt adapters.
- Credentials, tokens, and other secrets must stay out of domain/application code and static configuration embedded in source. Keep configuration in `appsettings` or external secure stores and wire them through infrastructure options.

## Testing strategy
- `tests/DevFund.TraktManager.Domain.Tests` exercise entity/value-object invariants.
- `tests/DevFund.TraktManager.Application.Tests` cover use-case coordination and presenter interactions via fakes.
- `tests/DevFund.TraktManager.Infrastructure.Tests` verify HTTP adapters against representative JSON payloads from `spec/trakt.apib`.
- `tests/DevFund.TraktManager.Presentation.Cli.Tests` assert console rendering and messaging.
- When adding a new layer artifact, create/extend the matching `*.Tests` project so every ring keeps parity.

## Presenter & implementer bindings
- Console CLI is the default presenter; register additional presenters (e.g., web APIs) by implementing `ICalendarPresenter` in their respective projects.
- Infrastructure HTTP clients must align with `.github/instructions/trakt-api.instructions.md`, which mirrors `spec/trakt.apib`. Reference that file whenever you add or modify endpoints.

## Collaboration pointers
- Follow `.github/copilot-instructions.md` for day-to-day workflows and build commands.
- Update this file after structural refactors so future contributors (or AI agents) stay synchronized.