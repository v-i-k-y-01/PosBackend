# PosBackend agent guide

## Project purpose and status

This is the backend for a **single-shop** point-of-sale system. It is an ASP.NET Core Web API targeting **.NET 10** with PostgreSQL (including Supabase) as its database. This repository contains backend code only: do not add or scaffold frontend code, multi-tenancy, payment gateways, tax/GST features, hardware integrations, loyalty features, email notifications, or supplier/purchase-order management.

The implementation is currently at **Step 1 (solution scaffold)**. The initial EF Core schema, Swagger, and `GET /api/health` exist. Auth, CRUD, sales, reports, authorization restrictions, and global exception handling remain to be implemented in this order:

1. Authentication and Owner/Cashier management.
2. Category CRUD.
3. Product CRUD and stock.
4. Transactional sales creation and stock deduction.
5. Sales history and reports.
6. Role-based restrictions.
7. Validation and consistent exception responses.

`README.md` at the repository root tracks progress. `docs/project-spec.md` is the source of truth for the agreed requirements; do not edit its preserved specification text. `docs/step-01-solution-scaffold.md` explains the existing foundation.

## Solution layout

The solution file is `PosBackend.sln` at the repository root. This guide is stored in `src/PosBackend.Api`, but most solution commands must be run from that root.

```
Domain          pure entities, enums, and base types
Application     use cases, DTOs, validation, interfaces, MediatR handlers
Infrastructure  EF Core/PostgreSQL persistence and external implementations
Api             HTTP controllers, middleware, authentication, composition root
```

Maintain the dependency direction exactly:

```
Api -> Application + Infrastructure -> Application -> Domain
```

- **Domain** must remain plain C# with no framework, database, HTTP, or outer-layer dependencies.
- **Application** may depend on Domain and defines abstractions such as `IAppDbContext`; it must not use concrete infrastructure implementations.
- **Infrastructure** implements Application contracts, owns EF Core mappings and migrations, and may depend on Application (therefore Domain transitively).
- **Api** owns HTTP concerns and dependency-injection composition. Keep controllers thin: translate HTTP concerns and dispatch application requests through MediatR.

## Existing conventions and important invariants

- C# uses nullable reference types and implicit usings (`net10.0`). Preserve both.
- All entities inherit `BaseEntity`, which assigns a `Guid` ID by default.
- Use `async` EF Core APIs and pass cancellation tokens through handlers.
- Application registers MediatR, AutoMapper, FluentValidation, and `ValidationBehavior<,>` in `AddApplication()`. Put new commands/queries, handlers, validators, DTOs, and mapping profiles in Application so assembly scanning finds them.
- The validation pipeline throws the custom `PosBackend.Application.Common.Exceptions.ValidationException`; do not bypass it with duplicate controller validation.
- Handlers should depend on `IAppDbContext`, never `AppDbContext` directly.
- `AppDbContext` auto-discovers `IEntityTypeConfiguration<>` classes. Keep persistence mapping in one focused configuration class per entity rather than adding EF attributes to Domain entities.
- Store money as `decimal`; EF mappings use `numeric(18,2)`.
- `UserRole` and `PaymentMethod` are stored as strings. Keep the values and conversions compatible with existing data.
- `Users.Email` and `Products.Sku` are unique. Product deletion is restricted when sale items exist; user deletion is restricted when sales exist; deleting a category sets `Product.CategoryId` to null; deleting a sale cascades to its items.
- Sales must calculate totals and unit-price snapshots server-side, validate stock, deduct stock, and create the sale plus items in one database transaction.
- The first registration creates the Owner; only an Owner creates Cashiers. Cashiers are limited to sales they own when role restrictions are added.
- Use UTC for persisted timestamps (`DateTime.UtcNow`).

## API conventions

- Routes use the `/api/...` prefix. The existing liveness endpoint is `GET /api/health` and must remain database-independent.
- Swagger is enabled only in Development and is the main manual API test surface at `/swagger`.
- Use controller-based APIs for feature endpoints and conventional REST status codes.
- Once exception middleware is introduced, preserve a consistent JSON error shape such as `{ "error": "message", "statusCode": 400 }`; map validation, unauthenticated, forbidden, not-found, and conflict cases to appropriate HTTP responses.
- Authentication is JWT Bearer with BCrypt password hashing. Read JWT values from configuration; never hardcode keys, credentials, or connection strings.

## Database and migrations

- Read the connection string from `ConnectionStrings:DefaultConnection`.
- Keep real secrets out of version control. Use environment variables or `dotnet user-secrets`; `appsettings.Development.json.example` is safe to update as a template.
- After changing an entity or EF configuration, create and commit a matching migration in `src/PosBackend.Infrastructure/Persistence/Migrations/`. Do not hand-edit generated migration designer files or the model snapshot unless repairing a generated migration.
- Use the Api as the EF startup project:

```bash
dotnet ef migrations add <Name> \
  --project src/PosBackend.Infrastructure \
  --startup-project src/PosBackend.Api

dotnet ef database update \
  --project src/PosBackend.Infrastructure \
  --startup-project src/PosBackend.Api
```

For Supabase migrations, prefer the Session-mode pooler on port 5432.

## Build, run, and verify

Run these from the repository root:

```bash
dotnet restore
dotnet build
dotnet run --project src/PosBackend.Api
```

The development profile listens on `http://localhost:5244`; verify `GET /api/health` and `/swagger`. There is currently no automated test project. Add focused tests alongside new behavior when introducing a test project, and at minimum build the full solution after changes.

The Dockerfile is `src/PosBackend.Api/Dockerfile`. Build it from the repository root because its build context needs all four projects:

```bash
docker build -f src/PosBackend.Api/Dockerfile -t posbackend .
```

Run it with `ConnectionStrings__DefaultConnection` supplied as an environment variable; never bake secrets into the image.

## Documentation and change hygiene

- Update the root README build-progress checklist and add/update a `docs/step-XX-*.md` explanation after completing a planned feature step.
- Keep changes scoped to the requested feature and preserve generated files unless the change requires regenerating them.
- Before handing off a change, run the relevant build and any available tests, then report commands and outcomes.

