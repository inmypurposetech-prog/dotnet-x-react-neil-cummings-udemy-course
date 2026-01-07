# Reactivities

Short guide and quick commands to reproduce the multi-project .NET + React example in this repo.

## What this repo contains
- `API/` — ASP.NET Web API (controllers, Program.cs)
- `Domain/` — domain models (Activity)
- `Persistence/` — EF Core DbContext, migrations, seeding
- `Application/` — app-level wiring (project references)
- `client/` — Vite + React + TypeScript + MUI frontend

## Quick run

1. Run API

```bash
cd API
dotnet run
```

2. Run client

```bash
cd client
npm install
npm run dev
```

3. Smoke test API endpoint

```bash
curl -k https://localhost:5001/api/activities
```

## Create & migrate (summary)

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project Persistence --startup-project API --output-dir Migrations
dotnet ef database update --project Persistence --startup-project API
```

## Project dependency diagram (ASCII)

Projects and their primary references (arrows point to referenced projects):

API      --> Persistence
API      --> Domain
Application --> Persistence
Application --> Domain
Persistence --> Domain

This diagram shows the intended separation of concerns: Domain is pure models; Persistence depends on Domain and provides EF Core; API wires up HTTP and depends on Persistence/Domain.

## Notes
- See `learnings-notebook.md` for a more detailed guide, rationale, and troubleshooting tips (CORS, dev certs, seeding).
- There is a likely CORS origin typo in `API/Program.cs` (`https://localhost;3000`) — change to `https://localhost:3000` if you hit CORS issues.
