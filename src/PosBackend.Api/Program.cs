using PosBackend.Application;
using PosBackend.Infrastructure;
using PosBackend.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;
using PosBackend.Api.Middleware;

// Handle inotify limitations in shared container hosting environments (e.g. Render, Koyeb)
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");

var builder = WebApplication.CreateBuilder(args);

// Dynamically bind to the PORT assigned by hosting providers (Render, Railway, Fly.io, etc.)
var envPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(envPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{envPort}");
}

// ==========================================
// 1. CONSTANTS DEFINITIONS
// ==========================================
const string CorsPolicyName = "Frontend";
const string TokenTypeClaim = "token_type";
const string AccessTokenValue = "access";
const string SecuritySchemeBearer = "Bearer";
const string SecurityHeaderAuthorization = "Authorization";

// ==========================================
// 2. LAYER COMPOSITION (CLEAN ARCHITECTURE)
// ==========================================
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ==========================================
// 3. AUTHENTICATION & AUTHORIZATION CONFIG
// ==========================================
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                // Enforce that refresh tokens cannot be used to invoke endpoints.
                if (context.Principal?.FindFirstValue(TokenTypeClaim) != AccessTokenValue)
                {
                    context.Fail("Only access tokens can be used to authorize API requests.");
                }
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                // Intercept default challenge response to return a consistent JSON schema.
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Authentication is required.",
                    statusCode = StatusCodes.Status401Unauthorized
                });
            },
            OnForbidden = async context =>
            {
                // Intercept default forbidden response to return a consistent JSON schema.
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "You do not have permission to perform this action.",
                    statusCode = StatusCodes.Status403Forbidden
                });
            }
        };
    });

builder.Services.AddAuthorization();

// ==========================================
// 4. CORS POLICY SETUP
// ==========================================
var rawOrigins = builder.Configuration["Cors:AllowedOrigins"] 
    ?? builder.Configuration["CORS_ALLOWED_ORIGINS"];

var configuredOrigins = !string.IsNullOrWhiteSpace(rawOrigins)
    ? rawOrigins.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

var finalOrigins = (configuredOrigins != null && configuredOrigins.Length > 0)
    ? configuredOrigins
    : ["http://localhost:5173", "http://127.0.0.1:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (finalOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(finalOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// ==========================================
// 5. WEB API & SWAGGER / OPENAPI DOCUMENTATION
// ==========================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(SecuritySchemeBearer, new OpenApiSecurityScheme
    {
        Name = SecurityHeaderAuthorization,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter a JWT access token."
    });
});

// ==========================================
// 6. REQUEST MIDDLEWARE PIPELINE
// ==========================================
var app = builder.Build();

// Optional automatic EF Core migration / reset at startup (convenient for cloud containers)
if (app.Configuration.GetValue<bool>("ResetDb") ||
    app.Configuration.GetValue<bool>("RESET_DB") ||
    args.Contains("--reset-db"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PosBackend.Infrastructure.Persistence.AppDbContext>();

    // In managed PostgreSQL (Supabase, Render, Neon), you cannot DROP the active database ('postgres').
    // Instead, drop all existing tables in the schema and re-run migrations from scratch.
    await db.Database.ExecuteSqlRawAsync(@"
        DO $$ DECLARE
            r RECORD;
        BEGIN
            FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = current_schema()) LOOP
                EXECUTE 'DROP TABLE IF EXISTS ""' || r.tablename || '"" CASCADE';
            END LOOP;
        END $$;
    ");
    await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync(db.Database);
}
else if (app.Configuration.GetValue<bool>("ApplyMigrations") ||
    app.Configuration.GetValue<bool>("APPLY_MIGRATIONS") ||
    args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PosBackend.Infrastructure.Persistence.AppDbContext>();
    await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync(db.Database);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors(CorsPolicyName);

if (app.Environment.IsDevelopment() || 
    app.Configuration.GetValue<bool>("EnableSwagger") || 
    app.Configuration.GetValue<bool>("ENABLE_SWAGGER"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness probe. Intentionally does NOT touch the database so it works
// even before migrations have been applied.
app.MapGet("/api/health", () =>
    Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
