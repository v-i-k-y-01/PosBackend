# Project Spec — Single-Shop POS Backend (original prompt)

> **Purpose:** This is the **original project specification, preserved verbatim**, so the full
> context is available even in a fresh session. Do not edit the content below — it is the source
> of truth for what we agreed to build.
>
> **Where we are:** progress is tracked in `README.md` → "Build progress", and each completed
> step has an explanatory doc in `docs/step-XX-*.md` (see `docs/step-01-solution-scaffold.md`).
>
> **Agreed deviation from the spec:** the spec says **.NET 8**, but only the **.NET 10** SDK is
> installed, so we build on **.NET 10** (approved). Code is essentially identical for this stack.

---

## The prompt (verbatim)

I want to build the backend for a single-shop POS (Point of Sale) system. Set up the full backend project from scratch using Clean Architecture, then implement features in the given order. Ask me clarifying questions only if something is truly ambiguous — otherwise make sensible decisions and keep moving. Do NOT touch or scaffold any frontend code in this phase — backend and published APIs only.

### Tech Stack

- **Language/Framework:** C#, ASP.NET Core Web API (.NET 8)
- **Architecture:** Clean Architecture — Domain, Application, Infrastructure, Api (Presentation) as separate class library projects referencing inward only
- **ORM:** Entity Framework Core with Npgsql.EntityFrameworkCore.PostgreSQL
- **Database:** PostgreSQL (I'll provide a Supabase connection string — use it via ConnectionStrings:DefaultConnection, managed through appsettings.Development.json locally and environment variables/user-secrets, never hardcoded)
- **Auth:** JWT Bearer auth (Microsoft.AspNetCore.Authentication.JwtBearer), passwords hashed with BCrypt.Net-Next
- **Validation:** FluentValidation, wired in via a pipeline/behavior if using MediatR, or as a filter otherwise
- **CQRS/Mediator (optional but preferred):** MediatR for request/handler separation in the Application layer — keeps controllers thin
- **Mapping:** AutoMapper (or manual mapping if you prefer simplicity — your call, just be consistent)
- **API docs:** Swashbuckle (Swagger UI enabled in dev) — this is our main testing tool for this phase since there's no frontend yet
- **Containerization:** Dockerfile for the Api project (for later free-tier hosting on Render/Fly.io)
- **IDE:** JetBrains Rider — use a .sln at the root referencing all projects so Rider's solution explorer works cleanly

### Tenancy model

Single-shop system — no multi-tenancy, no StoreId, no store-scoping logic anywhere. There is exactly one shop. Users have a Role: Owner or Cashier. The first user to register becomes the Owner; after that, public registration is locked — only an existing Owner can create new Cashier accounts via an authenticated endpoint.

### Solution / Folder Structure

Create this exact structure:

```
PosBackend/
├── PosBackend.sln
├── src/
│   ├── PosBackend.Domain/
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Category.cs
│   │   │   ├── Product.cs
│   │   │   ├── Sale.cs
│   │   │   └── SaleItem.cs
│   │   ├── Enums/
│   │   │   ├── UserRole.cs
│   │   │   └── PaymentMethod.cs
│   │   └── Common/
│   │       └── BaseEntity.cs
│   │
│   ├── PosBackend.Application/
│   │   ├── Common/
│   │   │   ├── Interfaces/           # IAppDbContext, ITokenService, ICurrentUserService, etc.
│   │   │   ├── Behaviors/            # ValidationBehavior, LoggingBehavior (if using MediatR)
│   │   │   └── Exceptions/           # NotFoundException, ValidationException, ForbiddenException
│   │   ├── Auth/
│   │   │   ├── Commands/             # RegisterOwnerCommand, LoginCommand, CreateCashierCommand
│   │   │   └── Dtos/
│   │   ├── Products/
│   │   │   ├── Commands/             # CreateProduct, UpdateProduct, DeleteProduct
│   │   │   ├── Queries/              # GetProducts, GetProductById
│   │   │   └── Dtos/
│   │   ├── Categories/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   └── Dtos/
│   │   ├── Sales/
│   │   │   ├── Commands/             # CreateSaleCommand (handles stock deduction transactionally)
│   │   │   ├── Queries/              # GetSalesHistory, GetSaleById
│   │   │   └── Dtos/
│   │   └── Reports/
│   │       ├── Queries/              # GetDailyRevenue, GetTopProducts
│   │       └── Dtos/
│   │
│   ├── PosBackend.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/       # EF Core IEntityTypeConfiguration per entity
│   │   │   └── Migrations/
│   │   ├── Auth/
│   │   │   └── TokenService.cs       # JWT generation/validation
│   │   ├── Services/
│   │   │   └── CurrentUserService.cs
│   │   └── DependencyInjection.cs    # AddInfrastructure() extension method
│   │
│   └── PosBackend.Api/
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── UsersController.cs
│       │   ├── ProductsController.cs
│       │   ├── CategoriesController.cs
│       │   ├── SalesController.cs
│       │   └── ReportsController.cs
│       ├── Middleware/
│       │   └── ExceptionHandlingMiddleware.cs
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Dockerfile
│
└── README.md
```

Dependency rule: Api → Application + Infrastructure → Application → Domain. Domain has zero dependencies on other layers. Application depends only on Domain and defines interfaces that Infrastructure implements.

### Database Schema (EF Core entities + migration)

- Users: Id, Email (unique), PasswordHash, Role (Owner|Cashier), CreatedAt
- Categories: Id, Name
- Products: Id, CategoryId (FK, nullable), Name, Sku, Price, StockQty, CreatedAt
- Sales: Id, CashierId (FK to Users), TotalAmount, PaymentMethod (Cash|Card|Upi), CreatedAt
- SaleItems: Id, SaleId (FK), ProductId (FK), Quantity, UnitPrice, Subtotal

No tenant scoping needed — standard EF Core relationships only.

### Feature Build Order

(implement in this sequence, one at a time, and pause for me to test each via Swagger before moving to the next)

1. **Solution scaffold** — create all 4 projects with correct references, wire up AppDbContext with the Supabase connection string, add initial EF Core migration, GET /api/health endpoint, confirm Swagger UI loads and the app runs in Rider
2. **Auth** — POST /api/auth/register (only succeeds if no Owner exists yet; creates the first Owner), POST /api/auth/login (returns JWT access + refresh token), JWT middleware wired into Program.cs, [Authorize(Roles = "Owner")] policy working, POST /api/users (Owner-only, creates Cashier accounts)
3. **Category CRUD** — full REST endpoints (GET/POST/PUT/DELETE /api/categories)
4. **Product CRUD** — full REST endpoints (GET/POST/PUT/DELETE /api/products), including stock quantity field
5. **Sales creation** — POST /api/sales accepting a list of { productId, quantity } + payment method; calculates totals server-side, decrements product stock, and creates Sale + SaleItem records inside a single DB transaction; returns the created sale with line items
6. **Sales history & reports** — GET /api/sales (list with pagination + date filter), GET /api/sales/{id}, GET /api/reports/daily-revenue, GET /api/reports/top-products
7. **Cashier restrictions** — verify Cashier role can only hit Sales endpoints (create/read own sales), not Products/Categories/Reports/Users management endpoints; Owner has full access
8. **Polish & validation** — FluentValidation on all commands (e.g. price > 0, stock >= 0, quantity > 0), global exception handling middleware returning consistent JSON error shape, proper HTTP status codes (400/401/403/404/409) throughout

### Non-functional requirements

- All secrets/config via environment variables / dotnet user-secrets in dev — provide appsettings.Development.json.example, never commit real secrets
- Consistent error response shape across all endpoints, e.g. `{ "error": "message", "statusCode": 400 }`
- Use async/await (DbContext async methods) throughout
- Root README.md explaining: how to restore/run in Rider, how to apply EF Core migrations (dotnet ef database update), how to set the connection string, and how to open Swagger UI (/swagger) to test endpoints
- Keep everything within free-tier limits — no paid dependencies, no external paid APIs

### What NOT to build in this phase

No frontend code of any kind. No multi-tenancy. No payment gateway integration, GST/tax compliance, barcode scanner hardware integration, loyalty/rewards, email notifications, or supplier/purchase order management. Skip these entirely for now.

Start with Step 1: Solution scaffold. Show me the folder/project structure once created and confirm the health-check endpoint + Swagger UI work in Rider before moving to Step 2. We will move to the React frontend as a separate, later phase once all backend APIs are built and verified.
