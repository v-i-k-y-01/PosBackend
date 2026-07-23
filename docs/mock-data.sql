-- PosBackend mock data for PostgreSQL / DBeaver.
-- Run AFTER applying the EF Core InitialCreate migration.
-- This script is idempotent: it only inserts rows with the fixed IDs below.
--
-- Login credentials created by this script:
--   owner@demo.local   / password
--   cashier@demo.local / password
--
-- The quoted table and column names are required because EF Core created
-- PascalCase PostgreSQL identifiers.

BEGIN;

-- 1. Users
-- BCrypt hash below is for the plaintext password: password
INSERT INTO "Users" ("Id", "Email", "PasswordHash", "Role", "CreatedAt")
VALUES
  ('10000000-0000-0000-0000-000000000001', 'owner@demo.local',
   '$2a$11$RlEFTfXu7sPZ6LENGM3i8uY6dN5XruT.qf2AwGivgd5bI4.XCpMEu', 'Owner',
   '2026-01-01 09:00:00+00'),
  ('10000000-0000-0000-0000-000000000002', 'cashier@demo.local',
   '$2a$11$RlEFTfXu7sPZ6LENGM3i8uY6dN5XruT.qf2AwGivgd5bI4.XCpMEu', 'Cashier',
   '2026-01-02 09:00:00+00')
ON CONFLICT ("Id") DO UPDATE
SET "Email" = EXCLUDED."Email",
    "PasswordHash" = EXCLUDED."PasswordHash",
    "Role" = EXCLUDED."Role",
    "CreatedAt" = EXCLUDED."CreatedAt";

-- 2. Categories
INSERT INTO "Categories" ("Id", "Name")
VALUES
  ('20000000-0000-0000-0000-000000000001', 'Beverages'),
  ('20000000-0000-0000-0000-000000000002', 'Snacks'),
  ('20000000-0000-0000-0000-000000000003', 'Dairy'),
  ('20000000-0000-0000-0000-000000000004', 'Household')
ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name";

-- 3. Products
INSERT INTO "Products" ("Id", "CategoryId", "Name", "Sku", "Price", "StockQty", "CreatedAt")
VALUES
  ('30000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'Mineral Water 1L', 'DEMO-WATER-1L', 20.00, 46, '2026-01-03 09:00:00+00'),
  ('30000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001', 'Cola Can 330ml', 'DEMO-COLA-330', 45.00, 37, '2026-01-03 09:00:00+00'),
  ('30000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000002', 'Salted Chips 100g', 'DEMO-CHIPS-100', 35.00, 28, '2026-01-03 09:00:00+00'),
  ('30000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000002', 'Chocolate Bar', 'DEMO-CHOC-50', 30.00, 39, '2026-01-03 09:00:00+00'),
  ('30000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000003', 'Whole Milk 1L', 'DEMO-MILK-1L', 60.00, 17, '2026-01-03 09:00:00+00'),
  ('30000000-0000-0000-0000-000000000006', '20000000-0000-0000-0000-000000000004', 'Dish Soap 500ml', 'DEMO-SOAP-500', 95.00, 14, '2026-01-03 09:00:00+00')
ON CONFLICT ("Id") DO UPDATE
SET "CategoryId" = EXCLUDED."CategoryId",
    "Name" = EXCLUDED."Name",
    "Sku" = EXCLUDED."Sku",
    "Price" = EXCLUDED."Price",
    "StockQty" = EXCLUDED."StockQty",
    "CreatedAt" = EXCLUDED."CreatedAt";

-- 4. Sales
INSERT INTO "Sales" ("Id", "CashierId", "TotalAmount", "PaymentMethod", "CreatedAt")
VALUES
  ('40000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002', 100.00, 'Cash', '2026-07-18 10:15:00+00'),
  ('40000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002', 140.00, 'Card', '2026-07-19 13:30:00+00'),
  ('40000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 155.00, 'Upi', '2026-07-20 18:45:00+00')
ON CONFLICT ("Id") DO UPDATE
SET "CashierId" = EXCLUDED."CashierId",
    "TotalAmount" = EXCLUDED."TotalAmount",
    "PaymentMethod" = EXCLUDED."PaymentMethod",
    "CreatedAt" = EXCLUDED."CreatedAt";

-- 5. Sale items. UnitPrice and Subtotal are price snapshots at sale time.
INSERT INTO "SaleItems" ("Id", "SaleId", "ProductId", "Quantity", "UnitPrice", "Subtotal")
VALUES
  ('50000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 2, 20.00, 40.00),
  ('50000000-0000-0000-0000-000000000002', '40000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000003', 1, 35.00, 35.00),
  ('50000000-0000-0000-0000-000000000003', '40000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000004', 1, 25.00, 25.00),
  ('50000000-0000-0000-0000-000000000004', '40000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000002', 2, 45.00, 90.00),
  ('50000000-0000-0000-0000-000000000005', '40000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000005', 1, 50.00, 50.00),
  ('50000000-0000-0000-0000-000000000006', '40000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000006', 1, 95.00, 95.00),
  ('50000000-0000-0000-0000-000000000007', '40000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000001', 3, 20.00, 60.00)
ON CONFLICT ("Id") DO UPDATE
SET "SaleId" = EXCLUDED."SaleId",
    "ProductId" = EXCLUDED."ProductId",
    "Quantity" = EXCLUDED."Quantity",
    "UnitPrice" = EXCLUDED."UnitPrice",
    "Subtotal" = EXCLUDED."Subtotal";

COMMIT;

-- Optional verification queries:
-- SELECT * FROM "Users" ORDER BY "CreatedAt";
-- SELECT * FROM "Categories" ORDER BY "Name";
-- SELECT * FROM "Products" ORDER BY "Name";
-- SELECT * FROM "Sales" ORDER BY "CreatedAt" DESC;
-- SELECT * FROM "SaleItems" ORDER BY "SaleId", "Id";
