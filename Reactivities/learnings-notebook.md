## Reactivities — Repeatable Workbook

Purpose: create a single, portable workbook that documents exactly what was built, why it was built that way, and step-by-step instructions you can paste into a new project to reproduce the same architecture and developer experience.

Keep this workbook in the repo root so future projects can reuse it or be used as a checklist when creating new microservices or monoliths using the same approach.

-----

## Top-level assumptions (change these when starting a new project)

- OS: macOS (commands use zsh). Adjust for Windows or Linux accordingly.
- .NET SDK: net10.0 (verify with `dotnet --version`).
- Node.js: v18+ and npm.
- Recommended dev tools: VS Code, dotnet-ef (global tool), and optionally mkcert for local HTTPS in Vite.

-----

## Project goals and constraints

- Keep domain models framework-agnostic (no EF attributes in `Domain`).
- Isolate persistence (EF Core) in `Persistence` project.
- Keep API concerns (routing, CORS, middleware) in `API` project.
- Use `Application` as the orchestration layer for Mediator/handlers/DTOs/AutoMapper.
- Provide a simple Vite + React + TypeScript frontend that consumes the API.

-----

## Full, repeatable setup (copyable step-by-step)

1) Create the solution and projects

```bash
mkdir reactivities && cd reactivities
dotnet new sln -n Reactivities

dotnet new webapi -n API
dotnet new classlib -n Domain
dotnet new classlib -n Persistence
dotnet new classlib -n Application

dotnet sln add API/API.csproj Domain/Domain.csproj Persistence/Persistence.csproj Application/Application.csproj
```

2) Add project references

```bash
dotnet add Application/Application.csproj reference Domain/Domain.csproj
dotnet add Application/Application.csproj reference Persistence/Persistence.csproj
dotnet add API/API.csproj reference Persistence/Persistence.csproj
dotnet add API/API.csproj reference Domain/Domain.csproj
```

3) Add EF Core and supporting packages

```bash
dotnet tool install --global dotnet-ef
cd Persistence
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
cd ..
dotnet add API package Microsoft.EntityFrameworkCore.Sqlite
```

4) Add developer helper packages (optional)

```bash
dotnet add Application package AutoMapper --version 16.0.0
dotnet add Application package MediatR --version 12.0.1   # optional if using CQRS
```

5) Create essential files (templates to copy)

- `Domain/Activity.cs` (POCO): example fields below.
- `Persistence/AppDbContext.cs` (DbContext wiring): register DbSet<Activity>.
- `Persistence/DbInitializer.cs` (seeder): idempotent seeding.
- `API/Controllers/ActivitiesController.cs` (GET endpoints).
- `API/Program.cs` — register services, CORS, DbContext, migration+seed on startup.

Minimal `appsettings.json` snippet (API project):

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Data Source=reactivities.db"
	},
	"Logging": { "LogLevel": { "Default": "Information" } }
}
```

6) Create and apply EF migrations (recommended for source control)

```bash
dotnet ef migrations add InitialCreate --project Persistence --startup-project API --output-dir Migrations
dotnet ef database update --project Persistence --startup-project API
```

7) Frontend quickstart (Vite + React + TS + MUI)

```bash
cd client
npm create vite@latest . -- --template react-ts
npm install
npm install axios @mui/material @mui/icons-material @emotion/react @emotion/styled @fontsource/roboto
npm run dev
```

Set axios base URL in client code or use full URL `https://localhost:5001/api/activities`.

-----

## Detailed file checklist (what to create/copy)

- Domain/Activity.cs
	- guid Id, Title, Description, Category, Date, City, Venue, Latitude, Longitude, IsCancelled

- Persistence/AppDbContext.cs
	- class AppDbContext : DbContext { public DbSet<Activity> Activities { get; set; } }

- Persistence/DbInitializer.cs
	- static async Task SeedData(AppDbContext ctx) { if (ctx.Activities.Any()) return; ctx.Activities.AddRange(sample); await ctx.SaveChangesAsync(); }

- API/Controllers/ActivitiesController.cs
	- GET /api/activities => return list
	- GET /api/activities/{id} => return single or 404

- API/Program.cs key parts
	- builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
	- builder.Services.AddCors();
	- app.UseCors(options => options.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:5173", "https://localhost:5173"));
	- migration + seed in a scope (context.Database.MigrateAsync(); DbInitializer.SeedData(context);)

Notes: Vite default port is 5173; the client originally used 3000 in CORS — standardize on 5173 or update CORS.

-----

## AutoMapper and Mediator notes

- AutoMapper is installed in `Application` to map Domain -> DTOs and DTO -> Domain; configure MappingProfiles in `Application/MappingProfiles.cs` and register in `Program.cs`:

```csharp
builder.Services.AddAutoMapper(typeof(MappingProfiles).Assembly);
```

- If using MediatR/CQRS, create feature folders under `Application/Activities/Commands` and `Application/Activities/Queries` and register MediatR in `Program.cs`.

-----

## Cancellation token (temporary implementation and removal)

If you added cancellation tokens during development, document their usage and provide a safe removal plan.

Where they appear:

- Controller action signatures: `public async Task<ActionResult<List<Activity>>> Get(CancellationToken cancellationToken)`
- EF Core calls: `await context.Activities.ToListAsync(cancellationToken)`
- Service methods: `public async Task<List<Activity>> GetAllAsync(CancellationToken cancellationToken)`

example implementation
```
    using System;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

namespace Application.Activities.Queries;
// will contain the logic to get the list of activities from the DB
public class GetActivityList
{
    // mediatr queries are structured by specifying a class within a class
    // query will contain any query parameters as properties
    public class Query : IRequest<List<Activity>> {}

    // Because we are using this handler to retrieve data from our database we need to inject/import the AppDbContext into the constructor of the handler
    // Handles and returns the request with a the IRequest that we specified in the Query class
    public class Handler(AppDbContext context, ILogger<GetActivityList> logger) : IRequestHandler<Query, List<Activity>>
    {
        public async Task<List<Activity>> Handle(Query request, CancellationToken cancellationToken)
        {

            // Cancellation tokens are theoretically a way to cancel ongoing operations
            // e.g where an operation is taking longer than expected to complete and a user
            // navigates away from a page before the operation completes then a cancellation token would be used to cancel the ongoing operation

            try
            {
                for (int i = 0; i < 10; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(1000, cancellationToken); // Simulate a delay
                    logger.LogInformation($"Processed activity {i}");
                }
            }
            catch (System.Exception)
            {
                
                logger.LogInformation("Task was cancelled");
            }

            return await context.Activities.ToListAsync(cancellationToken);
        }
    }
}
```

Removal steps (repeatable):

1. Search for tokens:

```bash
grep -R "CancellationToken" -n . || true
grep -R "ToListAsync(.*CancellationToken" -n . || true
```

2. Remove the token parameter from controllers and service signatures.
3. Remove token arguments from EF calls (use parameterless overloads).
4. Rebuild and run tests. Use `dotnet build` and `dotnet test` if you have tests.

Why remove immediately: partial token additions across layers can leave inconsistent APIs. If you intend to reintroduce tokens later, add them consistently across controller -> application -> persistence layers.

-----

## QA checklist before committing or copying to a new repo

- [ ] Build solution: `dotnet build` (root) — fix errors.
- [ ] Run API: `cd API && dotnet run` — confirm startup logs and port.
- [ ] Run client: `cd client && npm run dev` — confirm app loads.
- [ ] Hit endpoint: `curl -k https://localhost:5001/api/activities` — expect seeded JSON.
- [ ] Check migrations folder in `Persistence/Migrations` is present and committed.
- [ ] Verify CORS origin matches client URL.
- [ ] Run `dotnet ef database update` locally to ensure DB created.
- [ ] Run linter/tests for client (`npm run lint` if configured).

-----

## Recommended git workflow for replication projects

- Create a new repo from this template.
- Make small commits per concern: `feat(persistence): add AppDbContext and seed`.
- Keep migrations committed in `Persistence/Migrations`.
- Tag or branch when you have a stable scaffold: `git tag v0.1-scaffold`.

-----

## Troubleshooting common issues

- CORS blocked: check `API/Program.cs` origins list and client dev port.
- HTTPS certs: `dotnet dev-certs https --trust` (macOS will prompt). For Vite use `vite-plugin-mkcert`.
- EF errors migrating: delete DB file (if safe) and re-run `dotnet ef database update` after confirming migrations.

-----

## Appendix: Useful commands summary

```bash
# Build and run
dotnet build
cd API && dotnet run

# EF migrations
dotnet ef migrations add InitialCreate --project Persistence --startup-project API --output-dir Migrations
dotnet ef database update --project Persistence --startup-project API

# Frontend
cd client
npm install
npm run dev

# Search for cancellation token usage
grep -R "CancellationToken" -n . || true

# Trust dev certs (macOS)
dotnet dev-certs https --trust
```

-----

If you want, I will now:

- Search the repository for active `CancellationToken` usages and list the exact file locations, or
- Remove the cancellation token usages automatically (I will update code and run a build), or
- Fix the CORS origin typo in `API/Program.cs` and run a smoke test.

-----

## Preserved user notes & readings (your original content)

Below I preserved the personal notes and reading links you added earlier so nothing is lost. I left them verbatim to preserve your learning traces and TODOs.

Read more on clean architecture (here)[https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html]

### CQRS
- Command Query Responsibility Segregation
- Commands are one type of thing that does something with our database to update it
- Queries just read from the database
- Mediator mediates between the different layers in our clean architecture design of the application
- Commands
	- Do something
	- Modify state
	- Should return a value
	- e.g: Create, Edit or delete activity
- Queries
	- Answer a question
	- Do not modify state
	- Should return a value
	- e.g. Get list of activities
Readings TODO:
- (MS Learn)[https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs]
- (Martin Fowler)[https://martinfowler.com/bliki/CQRS.html]
- (Udi Dahan)[https://udidahan.com/2009/12/09/clarified-cqrs/]
- (Greg Young)[https://gist.github.com/meigwilym/025f08208b5640ad26bc410c8a83b10f]

### Mediator
- Allows Application to communicate to the API that the processing of the information has been done
- Responsible for receiving objects, outputting object back to the API
- Introducing a nuGET library to handle this for us - mediatr - (Terminal area -> nuget tab -> search 'Mediatr' install in Application (which the API has a dependency on therefore has access to))

#### Mediatr
- Used to create handlers for all our use cases in our application layer
- Working within the Application folder of the C# project
- For each feature we will create a folder for all the queries and commands
	- Fetch list of activities



