# DevFund Trakt Manager

DevFund Trakt Manager is a layered .NET 8 console application that surfaces Trakt calendar data using clean architecture boundaries.

## Solution layout

- `src/DevFund.TraktManager.Domain` — domain entities and value objects.
- `src/DevFund.TraktManager.Application` — use cases, contracts, and ports.
- `src/DevFund.TraktManager.Infrastructure` — HTTP adapters and DTOs aligned with the Trakt API.
- `src/DevFund.TraktManager.Presentation.Cli` — console host and presenter.
- `tests/*` — mirrored test projects providing coverage for each layer.

Refer to [`.github/instructions/architecture.instructions.md`](.github/instructions/architecture.instructions.md) for the authoritative dependency map and collaboration guidelines.

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download)
- Trakt API client credentials configured in `appsettings.json` before running the CLI.

## Getting started

```bash
cd /home/konsorted/dev/dev-fund-trakt-manager
dotnet restore
dotnet build
dotnet test
```

## Running the CLI presenter

1. Update `src/DevFund.TraktManager.Presentation.Cli/appsettings.json` with valid Trakt credentials.
2. Execute the CLI with optional filters:

```bash
cd /home/konsorted/dev/dev-fund-trakt-manager
dotnet run --project src/DevFund.TraktManager.Presentation.Cli -- --start=2024-01-01 --days=7
```

## Documentation & workflows

- Trakt API contract: [`.github/instructions/trakt-api.instructions.md`](.github/instructions/trakt-api.instructions.md)
- Copilot usage and conventions: [`.github/copilot-instructions.md`](.github/copilot-instructions.md)
- API blueprint reference: [`spec/trakt.apib`](spec/trakt.apib)
