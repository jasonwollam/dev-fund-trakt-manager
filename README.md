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
2. Execute the CLI with optional filters. When no valid access token is present, you'll receive a pairing code and verification URL to authorize the device before the requested data is fetched:

```bash
cd /home/konsorted/dev/dev-fund-trakt-manager
dotnet run --project src/DevFund.TraktManager.Presentation.Cli -- --start=2024-01-01 --days=7
```

### CLI modes & arguments

The CLI supports multiple modes selected through the `--mode` flag (defaults to `calendar`).

| Mode | Description | Core switches |
| --- | --- | --- |
| `calendar` | Shows upcoming episodes for your account. | `--start=yyyy-MM-dd`, `--days=N` |
| `watchlist` | Lists the items in your Trakt watchlist. | `--watchlist-type=all\|movies\|shows\|seasons\|episodes`, `--watchlist-sort=rank\|added\|...`, `--watchlist-order=asc\|desc` |
| `lists` | Explores personal/public lists, list items, and saved filters. | `--lists-kind=personal\|liked\|likes\|official\|saved`, `--lists-user=<slug\|me>`, `--lists-slug=<slug>`, `--lists-item-type=all\|movies\|shows\|seasons\|episodes\|people`, `--lists-include-items`, `--lists-page=N`, `--lists-limit=N`, `--lists-section=movies\|shows\|calendars\|search` |

Example watchlist command:

```bash
dotnet run --project src/DevFund.TraktManager.Presentation.Cli -- \
	--mode=watchlist \
	--watchlist-type=shows \
	--watchlist-sort=added \
	--watchlist-order=desc
```

All watchlist options map directly to Trakt's [`/sync/watchlist/{type}/{sort_by}/{sort_how}`](spec/trakt.apib) endpoint, so refer to the API blueprint for the full list of supported values.

### Lists mode usage

Use lists mode to audit your own lists, inspect a friend's curated list, or review saved filters that Trakt applies across discovery surfaces. The CLI accepts the following switches:

- `--lists-kind` *(default: `personal`)* — chooses the upstream endpoint.
  - `personal` → `/users/{user}/lists`
  - `liked` → `/users/{user}/lists/liked`
  - `likes` → `/users/{user}/likes/lists`
  - `official` → `/lists/official`
  - `saved` → `/users/saved_filters/{section}`
- `--lists-user=<slug|me>` — targets a specific user slug. Use `me` (default) for the authenticated account.
- `--lists-slug=<slug>` — when provided, fetches items for the specified list via `/users/{user}/lists/{slug}/items/{type}`.
- `--lists-item-type=all|movies|shows|seasons|episodes|people` *(default: `all`)* — narrows list items when `--lists-slug` is supplied.
- `--lists-include-items` — while browsing summaries (`personal`, `liked`, `official`), also load each list's items.
- `--lists-page` / `--lists-limit` — forwards pagination parameters to Trakt endpoints that support them.
- `--lists-section=movies|shows|calendars|search` — required when `--lists-kind=saved` to choose the saved filter section. Defaults to `movies` when omitted.

Example: enumerate your own personal lists with item previews.

```bash
dotnet run --project src/DevFund.TraktManager.Presentation.Cli -- \
	--mode=lists \
	--lists-kind=personal \
	--lists-user=me \
	--lists-include-items
```

Example: inspect a friend's "watch-next" list and show only the TV episodes it contains.

```bash
dotnet run --project src/DevFund.TraktManager.Presentation.Cli -- \
	--mode=lists \
	--lists-user=friend_slug \
	--lists-slug=watch-next \
	--lists-item-type=episodes
```

Example: review your saved filters for the Trakt discovery search surface.

```bash
dotnet run --project src/DevFund.TraktManager.Presentation.Cli -- \
	--mode=lists \
	--lists-kind=saved \
	--lists-section=search
```

The CLI maps these switches directly to the endpoints documented in [`spec/trakt.apib`](spec/trakt.apib). Consult the blueprint for full parameter lists and edge cases (e.g., VIP-only filters, pagination ceilings).

### Sample Trakt watchlist request

```http
GET /sync/watchlist/shows/added/desc HTTP/1.1
Host: api.trakt.tv
Content-Type: application/json
trakt-api-version: 2
trakt-api-key: <your-client-id>
Authorization: Bearer <your-access-token>
User-Agent: DevFund-Trakt-Manager/1.0
```

This request retrieves the authenticated user's show watchlist sorted by the most recently added items in descending order. Substitute the path segments to target other collection types or ordering strategies.

### Verifying a successful watchlist run

1. Execute the CLI using the watchlist mode command above. The tool will reuse any stored access token or, if needed, guide you through the Trakt device pairing flow before continuing.
2. When the request succeeds, the console renders a Spectre table labelled **Watchlist**. If no items match your filters, you'll instead see the message `No watchlist items returned for the selected criteria.`
3. Confirm that each row includes the rank, media type, item title, and the timestamp (local time) when it was added. Notes are shown only when Trakt returns non-empty text.

Example successful output (abbreviated):

```
 Watchlist
 ┌──────┬───────┬───────────────────────────────┬─────────────────┬───────┐
 │ Rank │ Type  │ Item                          │ Listed At       │ Notes │
 ├──────┼───────┼───────────────────────────────┼─────────────────┼───────┤
 │ 1    │ Show  │ Slow Horses · Season 3        │ 2025-09-12 18:04 │       │
 │ 2    │ Movie │ Dune: Part Two (2024)         │ 2025-08-22 21:17 │ IMAX  │
 └──────┴───────┴───────────────────────────────┴─────────────────┴───────┘
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
