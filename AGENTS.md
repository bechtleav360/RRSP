# AI Agent Instructions for RRSP

**Read [`Framework/AGENTS.md`](Framework/AGENTS.md) first** — it contains all Signum Framework conventions (entities, operations, logic, React components, localization, build system).

This file only covers RRSP-specific details.

---

## Project Structure
- **RRSP/** — Main library: entities, logic, and React components organized by module.
- **RRSP.Server/** — ASP.NET Core host, Vite dev server (port 3000), API controllers.
- **RRSP.Terminal/** — Console app for database migrations and data loading.
- **RRSP.Test.Logic/** — xUnit tests for business logic.
- **RRSP.Test.React/** — Selenium UI tests.
- **RRSP.Test.Environment/** — Shared test setup and database configuration.
- **Framework/** — Signum Framework git submodule (do not modify directly from this repo).

## Key Files
- `RRSP/Starter.cs` — Central bootstrapping. Registers all framework extensions and app modules via `Start()`.
- `RRSP/MainAdmin.tsx` — Imports and starts all module clients.
- `RRSP/Layout.tsx` — Main application shell (navbar, sidebar, modals).
- `RRSP.Server/Program.cs` — Server entry point, calls `Starter.Start()`.
- `Modules.xml` — Configuration for optional/removable modules.

## Build & Run
- **C#:** `dotnet build RRSP/RRSP.csproj` (not the entire solution).
- **TypeScript:** `yarn tsgo --build` from the RRSP folder.
- **Dev server:** `yarn dev` from RRSP.Server (Vite on port 3000).
- **Tests:** `dotnet test RRSP.Test.Logic/RRSP.Test.Logic.csproj`.