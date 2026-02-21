using PedidosAPI.API.DependencyInjection;
using PedidosAPI.API.Endpoints;
using PedidosAPI.API.Middleware;
using PedidosAPI.Infrastructure.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// OpenAPI nativo .NET 9
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title = "Pedidos API";
        doc.Info.Version = "v1";
        doc.Info.Description = "API REST para registro de pedidos con Clean Architecture, " +
                               "Entity Framework Core, Minimal API y Scalar.";
        doc.Info.Contact = new() { Name = "Backend Team" };
        return Task.CompletedTask;
    });
});

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();

// Spec JSON → /openapi/v1.json  (solo en desarrollo)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Scalar UI → /scalar/v1  (siempre disponible)
app.MapScalarApiReference(options =>
{
    options.WithTitle("Pedidos API")
           .WithTheme(ScalarTheme.Purple)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
           .WithOpenApiRoutePattern("/openapi/v1.json");
});

// Redirigir raíz "/" a Scalar automáticamente
app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();

// ── Minimal API Endpoints ─────────────────────────────────────────────────────
app.MapPedidosEndpoints();

// ── Migraciones automáticas (solo desarrollo) ─────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider
        .GetRequiredService<PedidosAPI.Infrastructure.Data.AppDbContext>();
    // db.Database.Migrate(); // Descomentar para aplicar migraciones al iniciar
}

app.Run();