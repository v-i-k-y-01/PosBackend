# Steps 3–8 — Core POS APIs

The backend now exposes the complete single-shop POS API. Owners manage categories, products, cashiers, and reports. Cashiers may create sales and retrieve only sales that they created.

- Categories and products use standard REST endpoints under `/api/categories` and `/api/products`.
- Product SKUs are unique; a category may be removed without deleting its products; products with sale history cannot be deleted.
- `POST /api/sales` takes `paymentMethod` (`Cash`, `Card`, or `Upi`) and `items` (`productId`, `quantity`). Prices and totals are calculated on the server and inventory is updated in one database transaction.
- Sales history is paginated and can be filtered by UTC date/time. Owners can view every sale; cashiers are scoped to their own records.
- Reports provide per-day revenue and top products, both restricted to Owners.
- FluentValidation runs through the MediatR pipeline. Application errors are normalized by API middleware to `{ error, statusCode, errors? }`.

Build verification:

```bash
dotnet build --no-restore
```

Before manual Swagger testing, apply the existing initial migration and configure a PostgreSQL connection string and a JWT key of at least 32 characters.
