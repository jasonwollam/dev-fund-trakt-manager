---
applyTo: '**/*'
---
# Trakt API integration guide

This document explains how to navigate Trakt’s API specification, design features around it, and keep our integration current. Treat it as the authoritative companion to `spec/trakt.apib` when building or reviewing infrastructure code.

## Canonical sources
- `spec/trakt.apib` (API Blueprint). Always diff against this file before changing HTTP contracts.
- Live reference: https://trakt.docs.apiary.io/api-description-document (mirrors the API Blueprint and exposes sample payloads).
- Automation: `scripts/update-api-spec.sh` should fetch the Apiary document and highlight changes in `spec/trakt.apib`.

## Reading the spec effectively
- **Headers** – Every request must include `Content-Type: application/json`, `trakt-api-key`, `trakt-api-version: 2`, and a descriptive `User-Agent`. Authenticated calls add `Authorization: Bearer <access_token>`.
- **Authentication flows** – Device flow is documented under *Authentication → Devices*. Standard OAuth lives under *Authentication → OAuth*. Confirm required scopes before invoking protected routes.
- **Rate limiting & VIP** – Inspect the `Rate Limiting`, `VIP Methods`, and `VIP Enhanced` sections in the spec. Surface limits in docs or CLI responses when a workflow risks throttling.
- **Pagination & filtering** – Shared query parameters (`page`, `limit`, `extended`, `filters`) are defined once in the spec; reuse them verbatim. Never invent custom filters.
- **Extended info** – Many endpoints support `extended=full`, `full,images`, etc. Add flags only when the application needs the extra fields and ensure DTOs are updated accordingly.
- **Standard objects** – Media payload definitions (`movie`, `show`, `season`, `episode`, `person`, `user`) appear in *Standard Media Objects*. Map these directly to DTOs/value objects to avoid drift.
- **Error handling** – HTTP status codes and error payload examples live in the *Errors* section. Propagate meaningful messages to the application layer and surface remediation tips in presenters.

## Feature slices ↔ spec sections
Use these vertical slices to plan work. Each slice touches all layers (Domain → Application → Infrastructure → Presentation) plus their sibling tests. The “Spec focus” column points to the areas of `spec/trakt.apib` to study first.

| Slice | Spec focus | Representative endpoints | Implementation notes |
| --- | --- | --- | --- |
| Device authentication & token lifecycle | Authentication → Devices / OAuth | `/oauth/device/code`, `/oauth/device/token`, `/oauth/token`, `/oauth/revoke` | Handles polling, token storage, and refresh workflows. Presenter must guide the user through verification UI. |
| Calendar consumption | Calendars | `/calendars/{scope}/{type}/{start_date}/{days}` variants | Date math, optional extended info, and timezone handling; results drive CLI calendar views. |
| Watchlist management | Sync → Watchlist, Users → Watchlist | `/sync/watchlist/*`, `/users/{id}/watchlist/*` | Supports CRUD plus ordering. Requires batch payloads and idempotent retries. |
| Collection & playback sync | Sync → Collection / Playback | `/sync/collection/*`, `/sync/playback/*` | Import/export scenarios with large payloads; mind rate limits and delta updates. |
| History & analytics | Sync → History, Users → History / Stats, Shows → Progress | `/sync/history/*`, `/users/{id}/history/*`, `/users/{id}/stats`, `/shows/{id}/progress/*` | Covers pagination-heavy reads and progress aggregation. |
| Lists & saved filters | Users → Lists / Saved Filters, Lists | `/users/{id}/lists/*`, `/lists/{id}/*`, `/users/saved_filters/{section}` | Exposes both user-owned lists and public list metadata; ensure CLI shows sorting options. |
| Recommendations & discovery | Movies → Trending/Popular, Shows → Trending/Popular, Recommendations | `/movies|shows/trending`, `/movies|shows/popular`, `/recommendations/*` | Extended query support (ignore collected/watchlisted). Ideal for CLI discovery modes. |
| Social interactions | Comments, Sync → Ratings, Users → Likes | `/comments/*`, `/sync/ratings/*`, `/users/{id}/likes/*` | Posting requires authenticated context and user feedback when moderation errors occur. |
| People & metadata enrichment | People, Shows/Movies → People, Metadata endpoints (Genres, Certifications) | `/people/{id}/*`, `/shows|movies/{id}/people`, `/genres/*`, `/certifications/*` | Enriches detail views with credits, genres, and certifications. |

## Endpoint map by category
The table below links high-level feature areas to their primary endpoints. Use it to verify coverage and to locate sample payloads inside `spec/trakt.apib` or the Apiary mirror.

| Category | Endpoint families | Notes |
| --- | --- | --- |
| Authentication | `/oauth/authorize`, `/oauth/token`, `/oauth/device/*`, `/oauth/revoke` | Includes both standard OAuth and device code flow. Confirm scopes and token lifetimes. |
| Calendars | `/calendars/{scope}/{type}/{start}/{days}` | Supports personal (`my`) and global (`all`) scopes for shows, movies, premieres, etc. |
| Check-in & scrobbling | `/checkin`, `/scrobble/start|pause|stop` | Mutate hydrated playback state; ensure only one active check-in per account. |
| Collections & sync | `/sync/collection/*`, `/sync/playback/*`, `/sync/last_activities` | Keep local caches aligned with Trakt; respect `last_activities` timestamps. |
| History & watched data | `/sync/history/*`, `/users/{id}/history/*`, `/shows/{id}/progress/*` | Provide date filters and pagination. Useful for analytics slices. |
| Lists & curation | `/users/{id}/lists/*`, `/lists/{id}/*`, `/users/saved_filters/{section}` | Handles personal lists, collaborations, saved filters, and comments. |
| Movies & shows metadata | `/movies/*`, `/shows/*`, `/shows/{id}/seasons`, `/shows/{id}/episodes` | Retrieve summary, aliases, releases, stats, related items, and people. |
| People & companies | `/people/{id}/*`, `/networks`, `/genres/*`, `/certifications/*`, `/countries/*`, `/languages/*` | Static vocab endpoints for enrichment and localization. |
| Recommendations & discovery | `/movies|shows/trending`, `/movies|shows/popular`, `/recommendations/*` | Input parameters control filtering (e.g., ignore collected). |
| Search & lookup | `/search/{type}`, `/search/{id_type}/{id}` | Text search as well as ID translation (IMDB, TMDB, etc.). |
| Social interactions | `/comments/*`, `/users/{id}/likes/*`, `/users/{id}/notes/*` | Includes threaded comments, likes, and notes. |
| User profile & settings | `/users/settings`, `/users/{id}` plus followers/following/friends | Combine with lists and history for dashboard features. |
| Watchlists & favorites | `/sync/watchlist/*`, `/sync/favorites/*`, `/users/{id}/watchlist/*`, `/users/{id}/favorites/*` | Provide add/remove/reorder operations and comment feeds. |

## Authentication workflows

### Device flow (limited-input clients)
Mirror the steps laid out under *Authentication → Devices* in `spec/trakt.apib`:

1. **Generate codes** – `POST /oauth/device/code` with the client ID (and optional secret). Persist `device_code`, `user_code`, `verification_url`, `expires_in`, and `interval`.
2. **Prompt the user** – Display the `user_code` and `verification_url`. Keep the prompt visible until the flow succeeds or expires.
3. **Poll for authorization** – `POST /oauth/device/token` with the device code at the provided `interval`. Stop when you receive a token or when `expires_in` elapses.
4. **Handle polling errors** – Respect the `error` string: `authorization_pending`, `slow_down` (add 5 seconds), `access_denied`, `expired_token`.
5. **Capture success** – On HTTP 200, persist `access_token`, `refresh_token`, and `expires_in`. Subsequent requests must include the bearer token alongside the standard headers.

### Standard OAuth flow
For applications that can open a browser:

1. Direct users to `/oauth/authorize` with `response_type=code`, the client ID, redirect URI, and optional state.
2. Exchange the authorization code via `POST /oauth/token` (`grant_type=authorization_code`).
3. Refresh tokens using `POST /oauth/token` (`grant_type=refresh_token`) before expiry.
4. Revoke refresh tokens using `POST /oauth/revoke` when users sign out.

## Implementation & testing reminders
- Follow the clean architecture rules in `.github/instructions/architecture.instructions.md`; infrastructure is the only layer that talks to Trakt directly.
- Expand the matching test project whenever you add a class to Domain/Application/Infrastructure/Presentation. The slice table above hints at the tests each workflow needs.
- When adding CLI features, document new arguments in `README.md` and keep the prompt in `.github/prompts/trakt-api-implementation.prompt.md` in sync.
- Monitor specification changes regularly via `scripts/update-api-spec.sh` and capture deltas in commit messages or changelogs.