---
applyTo: '**/*'
---
# Trakt slice catalog

Use this catalog to choose the next vertical slice of Trakt functionality. Each entry summarises scope, endpoints, layer impact, and verification steps. Update the status column as slices progress.

## Legend
- **Status**: `todo`, `in-progress`, `done`, or `blocked`
- **Layers touched**: Domain (D), Application (A), Infrastructure (I), Presentation (P)

## Slice overview

| Slice | Status | Primary endpoints | Layers touched | Notes |
| --- | --- | --- | --- | --- |
| Device authentication & token lifecycle | done | `/oauth/device/code`, `/oauth/device/token`, `/oauth/token`, `/oauth/revoke` | A, I, P | Baseline device auth flow and token refresh exist; revisit when adding new presenters or stores. |
| Calendar consumption | todo | `/calendars/{scope}/{type}/{start_date}/{days}` variants | D, A, I, P | Drive CLI calendar mode, including extended info toggles and date input validation. |
| Watchlist management | done | `/sync/watchlist/*`, `/users/{id}/watchlist/*` | D, A, I, P | Initial slice ships watchlist retrieval via CLI with sorting/filter support backed by `/sync/watchlist/{type}/{sort_by}/{sort_how}` plus presenter output. |
| Collection & playback sync | todo | `/sync/collection/*`, `/sync/playback/*`, `/sync/last_activities` | D, A, I, P | Facilitates import/export flows and playback resume. Watch out for rate limiting and large payloads. |
| History & analytics | todo | `/sync/history/*`, `/users/{id}/history/*`, `/users/{id}/stats`, `/shows/{id}/progress/*` | D, A, I, P | Focus on pagination, time filters, and progress aggregation. |
| Lists & saved filters | todo | `/users/{id}/lists/*`, `/lists/{id}/*`, `/users/saved_filters/{section}` | D, A, I, P | Includes collaborative lists and saved filters; ensure CLI surfaces sorting/filtering options. |
| Recommendations & discovery | todo | `/movies|shows/trending`, `/movies|shows/popular`, `/recommendations/*` | D, A, I, P | Provide discovery mode with ignore filters (collected/watchlisted). |
| Social interactions | todo | `/comments/*`, `/sync/ratings/*`, `/users/{id}/likes/*` | D, A, I, P | Allow posting/moderating comments and managing ratings. Confirm auth scopes. |
| People & metadata enrichment | todo | `/people/{id}/*`, `/shows|movies/{id}/people`, `/genres/*`, `/certifications/*` | D, A, I, P | Enrich detail views with credits and metadata; expose via CLI detail commands. |

## Slice detail templates

Use the template below when capturing deeper context for each slice. Populate the relevant section in this file for active work.

### <SLICE NAME>
- **Status**: todo
- **Goal**: 
- **Spec focus**: (e.g., *Calendars*, *Sync → Watchlist*)
- **Endpoints**: 
- **Assumptions / open questions**: 
- **Domain impact**: 
- **Application impact**: 
- **Infrastructure impact**: 
- **Presentation impact**: 
- **Testing checklist**:
  - Domain
  - Application
  - Infrastructure
  - Presentation
- **Docs to update**: README, architecture instructions, prompt, others
- **Follow-ups**: 

Populate one section per slice as you take them on. Keep completed slices for historical context, noting major decisions or future improvements.

### Watchlist management
- **Status**: done
- **Goal**: Fetch and render the authenticated user's watchlist with type filters, sort options, and presenter output that mirrors Trakt's `/sync/watchlist/{type}/{sort_by}/{sort_how}` contract.
- **Spec focus**: *Sync → Watchlist*
- **Endpoints**: `/sync/watchlist/{type}/{sort_by}/{sort_how}` (GET)
- **Assumptions / open questions**: Uses authenticated user's default watchlist; mutation endpoints (`/sync/watchlist` POST/DELETE) are candidates for a future slice.
- **Domain impact**: Added `WatchlistEntry` aggregate plus supporting value objects to describe movies, shows, seasons, and episodes with invariant guarantees.
- **Application impact**: Introduced `WatchlistRequest`, sort/filter enums, service, and orchestrator along with `ITraktWatchlistClient` and `IWatchlistPresenter` abstractions.
- **Infrastructure impact**: Implemented `TraktWatchlistClient`, DTO mappers, DI registration, and token handling aligned with spec headers.
- **Presentation impact**: CLI gained `watchlist` mode, parsing for filter/sort arguments, and `ConsoleWatchlistPresenter` Spectre table rendering.
- **Testing checklist**:
  - Domain ✓ (`WatchlistEntryTests`)
  - Application ✓ (`WatchlistServiceTests`, presenter orchestration)
  - Infrastructure ✓ (`TraktWatchlistClientTests` covering mapping, auth)
  - Presentation ✓ (`CliCommandStrategyTests` ensuring request wiring)
- **Docs to update**: README (CLI usage), this catalog, Trakt API instructions cross references.
- **Follow-ups**: Extend slice to cover add/remove/reorder workflows and expose per-user watchlist endpoints (`/users/{id}/watchlist/*`).
