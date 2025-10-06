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
- Trakt API client credentials configured in `appsettings.json` (`Trakt:ClientId` and `Trakt:ClientSecret`). The CLI guides you through the Trakt device authorization flow at runtime and uses any configured access token as a starting point.

## Getting started

```bash
cd /home/konsorted/dev/dev-fund-trakt-manager
dotnet restore
dotnet build
dotnet test
```

## Running the CLI presenter

1. Update `src/DevFund.TraktManager.Presentation.Cli/appsettings.json` with valid Trakt credentials.
2. Execute the CLI with optional filters. When no valid access token is present, you'll receive a pairing code and verification URL to authorize the device before the calendar is fetched:

```bash
cd /home/konsorted/dev/dev-fund-trakt-manager
dotnet run --project src/DevFund.TraktManager.Presentation.Cli -- --start=2024-01-01 --days=7
```

### Device authentication walkthrough

The CLI implements Trakt's [device authorization flow](https://trakt.docs.apiary.io/#reference/authentication-devices) so you can pair the tool without embedding credentials:

1. **Check existing tokens** — if `AccessToken` (and optionally `RefreshToken`) are present in `appsettings.json`, they're used immediately. Leave them blank to trigger a fresh pairing.
2. **Generate a device code** — the app displays a `user_code` and `verification_url`. Keep the CLI window open during this step.
3. **Authorize in a browser** — open the verification URL, sign in to Trakt, and enter the code exactly as shown. Trakt confirms when the device is linked.
4. **Wait for confirmation** — the CLI polls Trakt at the required interval. You'll see a success message once an access token is issued (or an error if the request is denied or expires).
5. **Token persistence** — the new token is cached in-memory for the lifetime of the process. To reuse it across runs, copy the values back into `appsettings.json` or plug in your own secure store via `ITraktAccessTokenStore`.

If you cancel the flow (Ctrl+C) before authorization completes, simply rerun the CLI to request a new code.

## Debugging the CLI presenter

- From VS Code, open the command palette and run **.NET: Generate Assets for Build and Debug** to create the default `.vscode/launch.json`.
- Select the `DevFund.TraktManager.Presentation.Cli` launch profile, update the `args` array with the same CLI switches you would pass on the command line, then press <kbd>F5</kbd> (or use **Run and Debug** → **Start Debugging**).
- To iterate quickly from the terminal while still attaching the debugger, build once and reuse the binaries:

```bash
cd /home/konsorted/dev/dev-fund-trakt-manager
dotnet build
dotnet run --project src/DevFund.TraktManager.Presentation.Cli --no-build -- --start=2024-01-01 --days=7
```
- Breakpoints in the presentation, application, and infrastructure projects all hit as expected because the launch profile references the CLI project, which transitively loads the other assemblies.

## Documentation & workflows

- Trakt API contract: [`.github/instructions/trakt-api.instructions.md`](.github/instructions/trakt-api.instructions.md)
- Copilot usage and conventions: [`.github/copilot-instructions.md`](.github/copilot-instructions.md)
- API blueprint reference: [`spec/trakt.apib`](spec/trakt.apib)
