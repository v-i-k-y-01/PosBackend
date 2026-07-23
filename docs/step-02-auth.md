# Step 2 — Authentication and user management

> Phase: **Backend auth** · Status: ✅ Complete · Prereq for: Step 3 (Category CRUD)

This step adds the single-shop account model:

- `POST /api/auth/register` creates the first and only Owner account. A later registration attempt returns `409 Conflict`.
- `POST /api/auth/login` validates the BCrypt password hash and returns signed access and refresh JWTs.
- `POST /api/users` creates Cashier accounts and requires an authenticated user with the `Owner` role.

JWT issuer, audience, signing key, and lifetimes are read from the `Jwt` configuration section. Set the signing key through user-secrets or environment configuration; it must be at least 32 characters. Swagger now provides the Bearer authorization control for testing Owner-protected endpoints.

Passwords are never returned or stored in plaintext. The Application layer defines the password-hashing and token interfaces, while Infrastructure provides BCrypt and JWT implementations.

## Swagger test sequence

1. Apply the existing migration and run the API.
2. Register `owner@example.com` with a password of at least eight characters.
3. Log in and copy `accessToken` into Swagger's **Authorize** dialog.
4. Create a Cashier through `POST /api/users`.

The next feature is Category CRUD.
