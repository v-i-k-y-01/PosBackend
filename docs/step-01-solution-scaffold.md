# Step 1 — Solution Scaffold

> Phase: **Backend setup** · Status: ✅ Complete & verified · Prereq for: Step 2 (Auth)

This document explains **what was built in Step 1, and why** — the architecture, each
layer's responsibility, the database schema, the migration, configuration/secrets, and the
decisions that were made. It's written so you can come back to it later and understand every
choice, not just what the code does.

---

## Table of contents

1. [Goals of Step 1](#1-goals-of-step-1)
2. [Clean Architecture — the mental model](#2-clean-architecture--the-mental-model)
3. [The dependency rule](#3-the-dependency-rule)
4. [Tech stack & resolved versions](#4-tech-stack--resolved-versions)
5. [Solution / folder structure](#5-solution--folder-structure)
6. [Layer-by-layer walkthrough](#6-layer-by-layer-walkthrough)
   - [Domain](#domain--the-heart-of-the-system)
   - [Application](#application--use-cases-and-contracts)
   - [Infrastructure](#infrastructure--the-machinery)
   - [Api](#api--the-front-door)
7. [Database schema](#7-database-schema)
8. [The EF Core migration](#8-the-ef-core-migration)
9. [Configuration & secret management](#9-configuration--secret-management)
10. [Swagger & the health endpoint](#10-swagger--the-health-endpoint)
11. [Docker](#11-docker)
12. [Verification — what was checked](#12-verification--what-was-checked)
13. [Key decisions made in Step 1](#13-key-decisions-made-in-step-1)
14. [How to run it](#14-how-to-run-it)
15. [What's next (Step 2)](#15-whats-next-step-2)

---

## 1. Goals of Step 1

The prompt's Step 1 asked for:

- All **4 projects** created with correct Clean-Architecture references.
- **EF Core** wired up to PostgreSQL via the `ConnectionStrings:DefaultConnection` setting.
- An **initial migration** for the full schema.
- A **`GET /api/health`** endpoint.
- Confirm the app **runs** and **Swagger UI loads** (in Rider) before moving on.

In short: a rock-solid foundation that compiles, runs, documents itself, and can hold every
feature we'll add in Steps 2–8.

---

## 2. Clean Architecture — the mental model

Clean Architecture arranges code into **concentric layers** where dependencies always point
**inward**, toward the most stable, business-essential code.

```
   ┌─────────────────────────────────────────┐
   │  Api (Presentation)                     │   controllers, HTTP, Swagger
   │  ┌───────────────────────────────────┐  │
   │  │  Infrastructure                   │  │   EF Core, PostgreSQL, JWT, BCrypt
   │  │  ┌─────────────────────────────┐  │  │
   │  │  │  Application                │  │  │   use cases, DTOs, validation, MediatR
   │  │  │  ┌───────────────────────┐  │  │  │
   │  │  │  │  Domain                │  │  │  │   entities, enums — pure business model
   │  │  │  └───────────────────────┘  │  │  │
   │  │  └─────────────────────────────┘  │  │
   │  └───────────────────────────────────┘  │
   └─────────────────────────────────────────┘
```

**Why bother?** It keeps the business rules (Domain) independent of databases, web servers,
and frameworks. You can swap Postgres for another database, or ASP.NET for another host,
without touching the core logic. It also makes the code testable and predictable: each piece
has one job and one direction it's allowed to look.

---

## 3. The dependency rule

> **Dependencies point inward only.** `Api → Application + Infrastructure → Application → Domain`.

Concretely:

- **Domain** has **zero** references to any other project or framework package. It is pure C#.
- **Application** references **Domain** only, and *defines* the interfaces (contracts) that
  Infrastructure will later implement — e.g. `IAppDbContext`.
- **Infrastructure** references **Application** (and, transitively, Domain). It *implements*
  those interfaces — e.g. `AppDbContext : DbContext, IAppDbContext`.
- **Api** references **Application + Infrastructure**, and wires everything together.

This is enforced at the `.csproj` level, so a wrong reference fails the build — you can't
accidentally let the Domain depend on EF Core.

---

## 4. Tech stack & resolved versions

The spec targeted **.NET 8**, but only the **.NET 10** SDK was installed. After confirming,
we agreed to build on **.NET 10** — the code is essentially identical for this stack, only the
target framework (`net10.0`) and a few package versions differ.

| Concern | Package | Version |
| --- | --- | --- |
| Web framework | `Microsoft.NET.Sdk.Web` (ASP.NET Core) | net10.0 |
| ORM | `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.2 |
| …which brings | `Microsoft.EntityFrameworkCore` | 10.0.9 |
| Migrations tool (CLI) | `dotnet-ef` (global tool) | 10.0.9 |
| CQRS / Mediator | `MediatR` | 14.2.0 |
| Validation | `FluentValidation` + `…DependencyInjectionExtensions` | 12.1.1 |
| Object mapping | `AutoMapper` | 16.2.0 |
| API docs | `Swashbuckle.AspNetCore` | 10.2.3 |
| Design-time support | `Microsoft.EntityFrameworkCore.Design` | 10.0.9 |

> Note: the default .NET 10 web template ships `Microsoft.AspNetCore.OpenApi` (which carried a
> known advisory). We **removed** it and replaced it with **Swashbuckle** as requested, which
> cleared the warning.

---

## 5. Solution / folder structure

```
PosBackend/
├── PosBackend.sln                 # solution (legacy .sln format, for Rider)
├── README.md
├── .gitignore  .dockerignore
├── docs/
│   └── step-01-solution-scaffold.md   ← this file
└── src/
    ├── PosBackend.Domain/
    │   ├── Common/BaseEntity.cs
    │   ├── Enums/{UserRole,PaymentMethod}.cs
    │   └── Entities/{User,Category,Product,Sale,SaleItem}.cs
    ├── PosBackend.Application/
    │   ├── Common/{Interfaces,Exceptions,Behaviors}/
    │   └── DependencyInjection.cs
    ├── PosBackend.Infrastructure/
    │   ├── Persistence/
    │   │   ├── AppDbContext.cs
    │   │   ├── Configurations/{User,Category,Product,Sale,SaleItem}Configuration.cs
    │   │   └── Migrations/<timestamp>_InitialCreate.cs (+ .Designer + ModelSnapshot)
    │   └── DependencyInjection.cs
    └── PosBackend.Api/
        ├── Program.cs
        ├── Properties/launchSettings.json
        ├── appsettings.json  appsettings.Development.json  appsettings.Development.json.example
        └── Dockerfile
```

> **Why legacy `.sln` instead of `.slnx`?** .NET 10's `dotnet new sln` defaults to the new
> `.slnx` format, but the spec asked for `PosBackend.sln` and the legacy `.sln` works in every
> Rider version without caveats. We generated it with `dotnet new sln --format sln`.

---

## 6. Layer-by-layer walkthrough

### Domain — the heart of the system

**Files:** `Common/BaseEntity.cs`, `Enums/*`, `Entities/*`

The Domain holds the business model in plain C#. No EF Core attributes, no HTTP, no NuGet
packages — it doesn't even know a database exists.

- **`BaseEntity`** — an abstract base giving every entity a `Guid Id` (assigned a fresh
  `Guid.NewGuid()` by default). Centralising this avoids repeating the `Id` property on every
  entity.
- **Enums** — `UserRole { Owner, Cashier }` and `PaymentMethod { Cash, Card, Upi }`. Plain
  enums; how they're *stored* is an Infrastructure concern (we store them as strings — see
  [Decisions](#13-key-decisions-made-in-step-1)).
- **Entities** carry the schema fields plus **navigation properties** for relationships, e.g.
  `Product.Category`, `Sale.Items`, `User.Sales`, `SaleItem.Product`. These describe the graph
  of the domain; EF Core maps them to foreign keys in Infrastructure.

**Example (`Sale.cs`):**
```csharp
public class Sale : BaseEntity
{
    public Guid CashierId { get; set; }
    public User? Cashier { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
```

### Application — use-cases and contracts

**Files:** `Common/Interfaces/IAppDbContext.cs`, `Common/Exceptions/*`,
`Common/Behaviors/ValidationBehavior.cs`, `DependencyInjection.cs`

Application defines **what the system does** (use cases / commands / queries) and the
**contracts** between layers, without knowing *how* they're implemented.

- **`IAppDbContext`** — an abstraction over the database: exposes the five `DbSet<T>`
  collections and `SaveChangesAsync`. Application and Api code depend on this **interface**, not
  on the concrete `AppDbContext`. That's what lets Infrastructure be swapped or mocked.
  *(Application references the EF Core package purely so the interface can name `DbSet<T>` — a
  standard, accepted trade-off in this style.)*
- **Exceptions** — `NotFoundException`, `ValidationException`, `ForbiddenException`. These let
  handlers signal business errors in a type-safe way; Step 8's middleware will translate them
  into consistent HTTP responses.
- **`ValidationBehavior<TRequest, TResponse>`** — a **MediatR pipeline behavior**. In the CQRS
  pattern (see box), every request flows `Controller → MediatR → Handler`. A pipeline behavior
  is middleware in that flow. This one runs all FluentValidation validators for the request
  *before* the handler, throwing `ValidationException` if any rule fails. Registered once, it
  automatically validates every command we add in later steps — no per-controller wiring.
- **`DependencyInjection.AddApplication()`** — a single extension method that registers, for the
  whole Application assembly: MediatR (handlers) + the validation behavior + FluentValidation
  validators + AutoMapper profiles.

> **CQRS / MediatR in one paragraph:** instead of calling a service method directly, a
> controller sends a *Request* object through MediatR, which routes it to a single *Handler*.
> "Commands" change state, "Queries" read. This keeps controllers thin and gives us a single
> seam (the pipeline) to add cross-cutting concerns like validation, logging, etc.

### Infrastructure — the machinery

**Files:** `Persistence/AppDbContext.cs`, `Persistence/Configurations/*`,
`DependencyInjection.cs`

Infrastructure implements the Application contracts using real frameworks.

- **`AppDbContext : DbContext, IAppDbContext`** — the EF Core database context. It declares the
  five `DbSet`s and, in `OnModelCreating`, calls
  `modelBuilder.ApplyConfigurationsFromAssembly(...)` — which auto-discovers every
  `IEntityTypeConfiguration<>` class in the assembly. That keeps entity mapping rules in
  separate, focused files instead of a giant `OnModelCreating` switch.
- **Configurations (one per entity)** — define table names, primary keys, required/length
  constraints, unique indexes (e.g. `Email`, `Sku`), the decimal precision (`numeric(18,2)`) for
  money, the enum→string conversions, and all foreign-key relationships with their delete
  behaviors:
  - `Product.Category` → `SetNull` (deleting a category blanks the product's category).
  - `SaleItem.Sale` → `Cascade` (deleting a sale removes its line items).
  - `Sale.Cashier` & `SaleItem.Product` → `Restrict` (can't delete a user/product that has sales).
- **`DependencyInjection.AddInfrastructure(IConfiguration)`** — registers `AppDbContext` against
  PostgreSQL via `UseNpgsql(connection-string-from-config)`, and binds `IAppDbContext` to it.

### Api — the front door

**Files:** `Program.cs`, `appsettings*.json`, `Properties/launchSettings.json`, `Dockerfile`

`Program.cs` is the composition root — the only place that knows about **all** layers:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

Then it conditionally enables Swagger in Development and maps the routes:

```csharp
app.MapControllers();
app.MapGet("/api/health", () =>
    Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
```

> The `/api/health` endpoint deliberately **does not touch the database** — it returns 200 as
> long as the process is alive. That means it works even before migrations are applied, which is
> exactly what a liveness probe should do.

`launchSettings.json` pins the dev URL to **`http://localhost:5244`** and sets
`launchUrl: swagger` so Rider opens Swagger automatically.

---

## 7. Database schema

EF Core built this model from the Domain entities + Infrastructure configurations:

| Table | Columns | Notes |
| --- | --- | --- |
| **Users** | `Id` (uuid), `Email` (varchar 256, **unique**), `PasswordHash` (text), `Role` (varchar 20), `CreatedAt` (timestamptz) | `Role` stored as string `"Owner"`/`"Cashier"` |
| **Categories** | `Id` (uuid), `Name` (varchar 200) | |
| **Products** | `Id`, `CategoryId` (uuid, **nullable**), `Name` (varchar 300), `Sku` (varchar 100, **unique**), `Price` (numeric(18,2)), `StockQty` (int), `CreatedAt` | FK to Categories (SetNull) |
| **Sales** | `Id`, `CashierId` (uuid), `TotalAmount` (numeric(18,2)), `PaymentMethod` (varchar 20), `CreatedAt` | FK to Users (Restrict); `PaymentMethod` as string |
| **SaleItems** | `Id`, `SaleId` (uuid), `ProductId` (uuid), `Quantity` (int), `UnitPrice` (numeric(18,2)), `Subtotal` (numeric(18,2)) | FK to Sales (Cascade), FK to Products (Restrict) |

This is a **single-shop** schema: there is no `StoreId`, no tenancy. There is exactly one shop;
`Users` just carry a `Role` (Owner vs Cashier).

---

## 8. The EF Core migration

Generated with:

```bash
dotnet ef migrations add InitialCreate \
  --project src/PosBackend.Infrastructure \
  --startup-project src/PosBackend.Api \
  --output-dir Persistence/Migrations
```

This produced three files under `src/PosBackend.Infrastructure/Persistence/Migrations/`:

- `20260707172025_InitialCreate.cs` — the `Up`/`Down` that creates/drops the five tables.
- `…Designer.cs` — a snapshot of the model at this migration.
- `AppDbContextModelSnapshot.cs` — the *current* model, used to compute the *next* migration's diff.

Creating a migration **does not connect to the database** — EF derives the SQL from the model
alone. Applying it (`dotnet ef database update`) is what actually creates the tables; that runs
in Step 2 once your Supabase connection is configured.

---

## 9. Configuration & secret management

The connection string is read from `ConnectionStrings:DefaultConnection`, resolved by the .NET
configuration system in priority order:

```
Environment variables  >  dotnet user-secrets (Dev)  >  appsettings.Development.json  >  appsettings.json
```

- **`appsettings.json`** — base settings only; **no** connection string (production uses env vars).
- **`appsettings.Development.json`** — ships a **local-Postgres placeholder** so dev "just
  works" without a real secret. It is **not** a secret, so it's safe to commit.
- **`appsettings.Development.json.example`** — a template showing the Supabase format (and the
  JWT section we'll use in Step 2).
- **Real Supabase password** → stored in **`dotnet user-secrets`** (kept *outside* the repo, at
  `~/.microsoft/usersecrets/`) or an environment variable — **never** committed.

> **Supabase tip:** use the **Session-mode pooler (port 5432)** for migrations; the
> Transaction-mode pooler (6543) is fine for the running app but can interfere with EF Core
> migrations.

---

## 10. Swagger & the health endpoint

Swashbuckle generates an OpenAPI document (`/swagger/v1/swagger.json`) and serves an interactive
UI at **`/swagger`**. Because there is no frontend yet, **Swagger is the primary testing tool**
for this whole phase — every endpoint added in Steps 2–8 will be testable there.

`AddEndpointsApiExplorer()` ensures minimal-API endpoints (like `/api/health`) are documented
alongside controllers.

---

## 11. Docker

A multi-stage `Dockerfile` lives at `src/PosBackend.Api/Dockerfile`:

1. **Build stage** (`sdk:10.0`) — restores packages (with layer-cached, per-project `COPY`s),
   then `dotnet publish` in Release.
2. **Runtime stage** (`aspnet:10.0`) — copies only the published output, exposes `8080`, sets
   `ASPNETCORE_URLS=http://+:8080`, runs `PosBackend.Api.dll`.

The connection string is supplied at run time via the `ConnectionStrings__DefaultConnection`
environment variable (note the **double underscore**, which .NET maps to the `:` section
separator). `.dockerignore` keeps `bin/`/`obj/` and IDE files out of the image.

Build context is the **repo root** (so all projects are available):
```bash
docker build -f src/PosBackend.Api/Dockerfile -t posbackend .
```

---

## 12. Verification — what was checked

| Check | Result |
| --- | --- |
| `dotnet build PosBackend.sln` | ✅ **0 errors, 0 warnings** |
| `dotnet ef migrations add InitialCreate` | ✅ 3 migration files generated; schema matches spec |
| Run the app | ✅ started, listening on `http://localhost:5244` |
| `GET /api/health` | ✅ `200 {"status":"healthy","timestamp":"…"}` |
| `GET /swagger/v1/swagger.json` | ✅ `200` |
| `GET /swagger/index.html` | ✅ `200` (Swagger UI loads) |
| `dotnet-ef` global tool | ✅ installed (10.0.9) |
| `PosBackend.sln` references all 4 projects | ✅ |

---

## 13. Key decisions made in Step 1

| Decision | Why |
| --- | --- |
| **.NET 10** (not .NET 8) | Only the .NET 10 SDK was installed; approved. Code is essentially identical for this stack. |
| **`Guid` primary keys** | Non-enumerable, globally unique, generated client-side. Good default; sale "numbers" can be added later if needed. |
| **Enums stored as strings** | `Owner`/`Cashier`/`Cash`/… read clearly in the DB and survive enum reordering. |
| **`DateTime` (UTC) → `timestamptz`** | Maps cleanly to PostgreSQL's timestamp-with-time-zone. |
| **Money as `numeric(18,2)`** | Exact decimal math — never floats for currency. |
| **EF Core referenced in Application** | Only so `IAppDbContext` can name `DbSet<T>`. Standard clean-architecture trade-off. |
| **Entity mapping in separate config files** | Keeps `OnModelCreating` tiny; each entity's rules in one place. |
| **Validation via a Mediatr pipeline behavior** | One registration validates every future command automatically. |
| **Health endpoint skips the DB** | A liveness probe must succeed even when the DB is down or un-migrated. |

---

## 14. How to run it

```bash
# from the repo root
dotnet restore
dotnet build

# (Step 2 will add) set your Supabase connection string in user-secrets, then:
dotnet ef database update --project src/PosBackend.Infrastructure --startup-project src/PosBackend.Api

# run
dotnet run --project src/PosBackend.Api
# → http://localhost:5244/swagger
```

In **Rider**: open `PosBackend.sln` → run the `PosBackend.Api` configuration → Swagger opens at
`http://localhost:5244/swagger`. Test `GET /api/health` → expect `200`.

---

## 15. What's next (Step 2)

Step 2 is **Authentication**:

- `POST /api/auth/register` — only succeeds if **no Owner exists yet**; creates the first Owner.
- `POST /api/auth/login` — returns a **JWT** access (+ refresh) token.
- JWT Bearer middleware wired into `Program.cs`; `[Authorize(Roles = "Owner")]` policy enforced.
- `POST /api/users` — **Owner-only**, creates Cashier accounts.

Before that, we need your **Supabase connection string** (via `dotnet user-secrets`) so the
`InitialCreate` migration can be applied and the Auth endpoints have a database to write to.
