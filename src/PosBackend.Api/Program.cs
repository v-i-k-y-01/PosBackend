using PosBackend.Application;
using PosBackend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Layer composition (Clean Architecture) ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- Web API + OpenAPI docs ---
// Swagger UI is the primary testing tool in this backend-only phase.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Liveness probe. Intentionally does NOT touch the database so it works
// even before migrations have been applied.
app.MapGet("/api/health", () =>
    Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
