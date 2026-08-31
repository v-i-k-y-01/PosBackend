# 🚀 Free Deployment Guide for POS Application

This project is configured for 100% free hosting using:
- **Database**: [Supabase](https://supabase.com) (Free Managed PostgreSQL)
- **Backend**: [Render](https://render.com) (Free Docker Web Service with automatic HTTPS)
- **Frontend**: [Vercel](https://vercel.com) (Free Global Edge Hosting)

---

## 1. Free PostgreSQL Database Setup (Supabase)

1. Sign up or log into **[Supabase](https://supabase.com)**.
2. Click **New project**, choose an organization, project name, and strong database password.
3. Select a region close to your target audience.
4. Once the project is created:
   - Navigate to **Project Settings** (gear icon) → **Database**.
   - Under **Connection parameters**, locate the **Connection string** (URI).
   - Copy the URI string. It looks like:
     ```
     Host=aws-0-us-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.yourprojectref;Password=yourpassword;
     ```
   *(Note: Use port `5432` Session mode)*

---

## 2. Free Backend API Deployment (Render)

1. Ensure your latest code is pushed to GitHub:
   ```bash
   git add .
   git commit -m "Configure cloud hosting and deployment blueprints"
   git push origin main
   ```
2. Log into **[Render](https://render.com)** and connect your GitHub account.
3. Click **New +** → **Web Service**.
4. Choose **Build and deploy from a Git repository** and select `PosBackend`.
5. Configure the service settings:
   - **Name**: `posbackend-api` (or any unique name)
   - **Region**: Closest to your database region (e.g. Oregon)
   - **Language / Runtime**: `Docker`
   - **Dockerfile Path**: `src/PosBackend.Api/Dockerfile`
   - **Docker Context**: `.`
   - **Instance Type**: `Free`
6. Under **Environment Variables**, add the following:
   | Key | Value | Description |
   | :--- | :--- | :--- |
   | `ConnectionStrings__DefaultConnection` | `Host=...;Port=5432;Database=postgres;Username=...;Password=...` | Your Supabase connection string |
   | `Jwt__Key` | `a_super_secret_jwt_key_that_is_at_least_32_characters_long!` | Secret key for JWT signing |
   | `Jwt__Issuer` | `PosBackend` | Token issuer |
   | `Jwt__Audience` | `PosBackend` | Token audience |
   | `Cors__AllowedOrigins` | `*` *(or your Vercel URL once created)* | Allowed frontend URLs |
   | `APPLY_MIGRATIONS` | `true` | Automatically runs EF Core schema migrations upon container start |
   | `ENABLE_SWAGGER` | `true` | Enables Swagger UI at `/swagger` in production |
7. Click **Create Web Service**.
8. Once the build completes, copy your live backend URL (e.g. `https://posbackend-api.onrender.com`).
   - Test health endpoint: `https://posbackend-api.onrender.com/api/health`
   - Test Swagger documentation: `https://posbackend-api.onrender.com/swagger`

---

## 3. Free Frontend UI Deployment (Vercel)

1. Log into **[Vercel](https://vercel.com)** and click **Add New...** → **Project**.
2. Import your `PosBackend` GitHub repository.
3. In the project setup form:
   - **Framework Preset**: `Vite`
   - **Root Directory**: Click **Edit** and set it to `frontend`
   - **Build Command**: `npm run build`
   - **Output Directory**: `dist`
4. Expand **Environment Variables** and add:
   - **Name**: `VITE_API_URL`
   - **Value**: `https://posbackend-api.onrender.com` *(your live Render API URL from Step 2, no trailing slash)*
5. Click **Deploy**.
6. Vercel will build and assign you a URL like `https://posbackend-frontend.vercel.app`.

---

## 4. (Optional) Lock Down CORS

For production security, once your Vercel domain is live:
1. Go to your Render service dashboard → **Environment Variables**.
2. Update `Cors__AllowedOrigins` from `*` to `https://posbackend-frontend.vercel.app`.
3. Save changes — Render will redeploy automatically in seconds.
