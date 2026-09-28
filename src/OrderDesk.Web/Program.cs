using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Services;
using System.Text.Json.Serialization;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var databaseConnection = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException("Falta configurar la conexión PostgreSQL.");

// Render entrega DATABASE_URL como URI; Npgsql utiliza el formato clave=valor.
if (Uri.TryCreate(databaseConnection, UriKind.Absolute, out var databaseUri)
    && databaseUri.Scheme.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
{
    var credentials = databaseUri.UserInfo.Split(':', 2);
    databaseConnection = new NpgsqlConnectionStringBuilder
    {
        Host = databaseUri.Host,
        Port = databaseUri.Port,
        Database = databaseUri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
        SslMode = SslMode.Prefer
    }.ConnectionString;
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(databaseConnection));
builder.Services.AddHttpClient<IExternalCustomerService, ExternalCustomerService>(client =>
{
    var externalBaseUrl = builder.Configuration["ExternalApi:BaseUrl"];
    var externalHost = builder.Configuration["ExternalApi:Host"];
    client.BaseAddress = new Uri(externalBaseUrl ?? (externalHost is null ? "http://localhost:5081" : $"http://{externalHost}"));
    client.Timeout = TimeSpan.FromSeconds(3);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/error", () => Results.Problem("Ocurrió un error inesperado."));
app.MapControllerRoute(name: "default", pattern: "{controller=Customers}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.Run();
