# Copilot Instructions

## Project snapshot
- Single .NET 8 console app defined by `dev-fund-trakt-manager.csproj`; output type is `Exe` with implicit usings and nullable reference types enabled.
- Entry point lives in `Program.cs` using top-level statements; prefer extending this file or factoring logic into new classes under the `dev_fund_trakt_manager` namespace.
- Build artifacts land in `bin/Debug/net8.0/`; avoid committing generated files under `bin/` or `obj/`.

## Core workflows
- Restore/build locally with:
  ```bash
  dotnet restore
  dotnet build
  dotnet run --project dev-fund-trakt-manager.csproj
  ```
- Tests are not yet present; if you add them, create a sibling `*.Tests` project and wire it into the solution so `dotnet test` passes.
- Update NuGet dependencies through `dotnet add package <name>`; ensure versions remain compatible with `net8.0`.

## Conventions & patterns
- Stick to C# 12 features available in .NET 8; keep the current top-level program style unless refactoring into a full `Main` improves clarity.
- Add new `.cs` files next to `Program.cs` (or in subfolders you create) and align namespaces with the default `dev_fund_trakt_manager` root.
- Keep `Program.cs` focused on wiring and invoke surrounding classes for substantive logic to keep the entry point readable.
- Document any Trakt-specific endpoints or workflows inline when you add them, since no reference implementation exists yet.
