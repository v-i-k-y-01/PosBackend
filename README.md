# PosBackend

Single-shop **Point-of-Sale (POS)** backend — an ASP.NET Core Web API built with **Clean Architecture** on **.NET 10**, backed by **PostgreSQL** (Supabase).

> This repository is the **backend only**. A React frontend will be added in a separate, later phase once all APIs are built and verified.

## Tech stack

| Concern | Choice |
| --- | --- |
| Language / Web framework | C# / ASP.NET Core Web API (.NET 10) |
| Architecture | Clean Architecture — Domain → Application → Infrastructure → Api |
| ORM | Entity Framework Core 10 + Npgsql (PostgreSQL) |
| Database | PostgreSQL (Supabase) |
| Auth | JWT Bearer + BCrypt password hashing |
| Validation | FluentValidation, run via a MediatR pipeline behavior |
| CQRS / Mediator | MediatR |
| Mapping | AutoMapper |
| API docs | Swashbuckle (Swagger UI) |
| Container | Docker (Api project) |

## Solution structure

```
PosBackend/
├── PosBackend.sln
├── src/
│   ├── PosBackend.Domain/         # Entities, enums, base types — zero dependencies
│   ├── PosBackend.Application/    # Interfaces, behaviors, commands/queries, DTOs
│   ├── PosBackend.Infrastructure/ # EF Core DbContext, configurations, services
│   └── PosBackend.Api/            # Controllers, middleware, Program.cs, Swagger
└── README.md
```

**Dependency rule:** `Api → Application + Infrastructure → Application → Domain`.
The Domain layer has no dependencies on any other layer. Application depends only on Domain and defines interfaces (`IAppDbContext`, …) that Infrastructure implements.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — check with `dotnet --version`
- EF Core CLI tool: `dotnet tool install --global dotnet-ef`
- PostgreSQL — local, or a Supabase project
- (Optional) JetBrains Rider, Docker

## 1. Restore & build

```bash
dotnet restore
dotnet build
```

Or open `PosBackend.sln` in **Rider** and build the solution.

## 2. Configure the database connection

The connection string is read from `ConnectionStrings:DefaultConnection`. **Real secrets are never committed.** Set it one of two ways:

### Option A — user-secrets (recommended for local dev)

```bash
cd src/PosBackend.Api
dotnet user-secrets init   # only once
dotnet user-secrets set ConnectionStrings:DefaultConnection \
  "Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<password>"
dotnet user-secrets set Jwt:Key "<a-random-secret-at-least-32-characters-long>"
```

### Option B — edit `appsettings.Development.json`

The file ships with a local-Postgres placeholder. Replace `DefaultConnection` with your Supabase string. **Do not commit a real password here.**

A template with the Supabase format (and upcoming JWT settings) is in `appsettings.Development.json.example`.

> **Supabase tip:** use the **Session mode** pooler (port **5432**) for applying EF Core migrations; the **Transaction mode** pooler (port 6543) is fine for the running app but can interfere with migrations.

## 3. Apply the database schema (EF Core migrations)

From the repository root:

```bash
dotnet ef database update \
  --project src/PosBackend.Infrastructure \
  --startup-project src/PosBackend.Api
```

This applies `Migrations/<InitialCreate>`, creating the `Users`, `Categories`, `Products`, `Sales`, and `SaleItems` tables.

To add a new migration after changing an entity:

```bash
dotnet ef migrations add <Name> \
  --project src/PosBackend.Infrastructure \
  --startup-project src/PosBackend.Api
```

## 4. Run the API

```bash
dotnet run --project src/PosBackend.Api
```

Rider: select the **`PosBackend.Api`** run configuration and press Run. The app listens on **`http://localhost:5244`**.

## 5. Open Swagger UI (primary testing tool)

Browse to **http://localhost:5244/swagger**.

Smoke test: `GET /api/health` → `200 { "status": "healthy", "timestamp": "…" }`.

## Auth API (Step 2)

Use Swagger to test these endpoints in order:

1. `POST /api/auth/register` with `{ "email": "owner@example.com", "password": "password123" }` creates the one Owner account. Registration returns `409` once an Owner exists.
2. `POST /api/auth/login` with the same credentials returns access and refresh JWTs. Use the **Authorize** button in Swagger with the access token.
3. `POST /api/users` with `{ "email": "cashier@example.com", "password": "password123" }` creates a Cashier and requires an Owner access token.

Set `Jwt:Key` through user-secrets in development (it must be at least 32 characters). The committed development configuration contains a safe placeholder only.

## Docker

Build from the repository root (context must include all projects):

```bash
docker build -f src/PosBackend.Api/Dockerfile -t posbackend .
docker run -d -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="<your-connection-string>" \
  posbackend
```

## Build progress

Features are implemented one at a time and verified via Swagger before moving on:

- [x] **Step 1** — Solution scaffold, EF Core migration, `GET /api/health`, Swagger UI
- [x] **Step 2** — Auth: register first Owner, login (JWT), create Cashier
- [x] **Step 3** — Owner-only category CRUD
- [x] **Step 4** — Owner-only product CRUD (category, SKU, price, stock quantity)
- [x] **Step 5** — Transactional sales creation with server-side totals and stock deduction
- [x] **Step 6** — Sales history (pagination/date filters) and owner reports
- [x] **Step 7** — Owner/Cashier restrictions (cashiers create and view only their sales)
- [x] **Step 8** — FluentValidation and consistent JSON error handling

## POS API quick reference

All write and management endpoints require an access JWT returned from `POST /api/auth/login`.

| Area | Endpoints | Access |
| --- | --- | --- |
| Users | `POST /api/users/owners` (additional Owner), `POST /api/users` (Cashier) | Owner |
| Categories | `GET/POST/PUT/DELETE /api/categories` | Owner |
| Products | `GET/POST/PUT/DELETE /api/products` | Owner |
| Sales | `POST /api/sales`, `GET /api/sales`, `GET /api/sales/{id}` | Owner or Cashier (cashiers see only their own) |
| Reports | `GET /api/reports/daily-revenue`, `GET /api/reports/top-products` | Owner |

`GET /api/sales` accepts `page`, `pageSize`, `from`, and `to`. Product listing accepts `categoryId` and `search`. Errors use `{ "error": "…", "statusCode": 400 }`, with an `errors` object for validation failures.
