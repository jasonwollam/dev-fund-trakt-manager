---
mode: agent
---
# Trakt API implementation playbook

Use this checklist whenever you add or extend functionality that talks to `trakt.tv`. Follow the clean architecture boundaries documented throughout the repo and keep every layer and its sibling test project in sync.

## Authoritative references
- Clean architecture rules: [`.github/instructions/architecture.instructions.md`](../instructions/architecture.instructions.md)
- HTTP contract details: [`.github/instructions/trakt-api.instructions.md`](../instructions/trakt-api.instructions.md), `spec/trakt.apib`, and the live spec at https://trakt.docs.apiary.io/api-description-document
- Daily workflow commands: [`.github/copilot-instructions.md`](../copilot-instructions.md)
- Project overview and usage: [`README.md`](../../README.md)

## Workflow overview
1. **Scope & plan** – Pick the Trakt capability you’re implementing, study the spec, and decide which layers need updates.
2. **Model the domain** – Introduce or adjust entities/value objects so the domain expresses the new behaviour without infrastructure concerns.
3. **Shape the application layer** – Add contracts, DTOs, and ports that describe the use case and how presenters consume it.
4. **Implement infrastructure** – Build HTTP clients, request/response mappers, DI wiring, and auth handling that satisfy the ports.
5. **Wire up presentation** – Extend CLI (or other presenters) with arguments/formatting that surface the new data.
6. **Test every ring** – Mirror production code with unit/integration tests in the matching `tests/*` project.
7. **Document & automate** – Update docs (README, architecture, this playbook) and maintain the spec-monitoring script.

Treat these steps as incremental checkpoints; complete each before moving outward to the next layer.

## Detailed guidance

### 1. Scope & plan
- Inspect the target endpoint(s) in `spec/trakt.apib` and the Apiary mirror.
- Capture assumptions, required auth scopes, rate limits, and pagination rules.
- If the API contract changed, update the spec monitoring script and note the change in documentation.

### 2. Domain design
- Introduce new entities/value objects only when behaviour requires it; keep them persistence-agnostic.
- Preserve existing invariants and ensure equality/validation rules remain consistent.
- Add or update domain tests under `tests/DevFund.TraktManager.Domain.Tests` to document new invariants.

### 3. Application layer
- Define request/response contracts, orchestrators, and ports (e.g., `ITrakt*` clients, presenters).
- Ensure dependency direction flows Domain ← Application; avoid referencing infrastructure types.
- Expand tests in `tests/DevFund.TraktManager.Application.Tests` with fakes/mocks that exercise new orchestration paths.

### 4. Infrastructure layer
- Implement HTTP adapters that satisfy the application ports, using DTOs aligned with `spec/trakt.apib`.
- Respect headers, auth flows, pagination, and rate-limiting guidance from the instructions file.
- Register new services via the infrastructure DI extension and cover them in `tests/DevFund.TraktManager.Infrastructure.Tests`.

### 5. Presentation layer
- Extend CLI option parsing and presenters so users can reach the new functionality (e.g., new `--mode` or flags).
- Keep console output consistent with existing styling.
- Add or modify tests in `tests/DevFund.TraktManager.Presentation.Cli.Tests` to validate user interactions.

### 6. Documentation & communication
- Update `README.md`, architecture instructions, and other guides when workflows or structures change.
- Note any new configuration requirements (API keys, CLI arguments, environment variables).
- If this playbook itself needs changes, edit it as part of the same change set.

### 7. API specification automation
- Maintain `scripts/update-api-spec.sh` so it polls https://trakt.docs.apiary.io/api-description-document for updates and assists with diffing `spec/trakt.apib`.
- Enhance the script when new automation opportunities appear (e.g., caching, notifications).

## Validation checklist
- `dotnet restore`, `dotnet build`, and `dotnet test` succeed from the repository root.
- New or modified CLI commands run end-to-end with representative arguments.
- All linters/formatters configured in the repo remain green.

## Style & hygiene
- Follow existing naming, nullability, async, and dependency-injection patterns.
- Prefer constructor injection with cancellation tokens and avoid static/global state.
- Keep commits and pull requests focused on a single workflow for easier review.
