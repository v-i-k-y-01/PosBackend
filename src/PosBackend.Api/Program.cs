using PosBackend.Application;
using PosBackend.Infrastructure;
using PosBackend.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;
using PosBackend.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://127.0.0.1:5173"])
        .AllowAnyHeader()
        .AllowAnyMethod());
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

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors(CorsPolicyName);

if (app.Environment.IsDevelopment())
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
