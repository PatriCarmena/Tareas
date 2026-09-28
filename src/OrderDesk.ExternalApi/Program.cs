var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var segments = new[] { "Minorista", "Mayorista", "Empresa", "Preferencial" };
app.MapGet("/api/customer-insights/{customerId:int}", (int customerId) =>
{
    var score = 50 + Math.Abs(customerId * 17 % 50);
    return Results.Ok(new
    {
        customerId,
        riskScore = score,
        segment = segments[Math.Abs(customerId) % segments.Length],
        recommendation = score >= 80 ? "Apto para beneficios preferenciales" : "Seguimiento comercial estándar",
        checkedAtUtc = DateTime.UtcNow
    });
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
