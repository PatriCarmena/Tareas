using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
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
        Port = databaseUri.IsDefaultPort ? 5432 : databaseUri.Port,
        Database = databaseUri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
        SslMode = SslMode.Prefer
    }.ConnectionString;
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(databaseConnection));
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}
builder.Services.AddHttpClient<IWhatsAppSender, TwilioWhatsAppSender>();
builder.Services.AddHttpClient<IEmailSender, BrevoEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.brevo.com/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient<IExternalCustomerService, ExternalCustomerService>(client =>
{
    var externalBaseUrl = builder.Configuration["ExternalApi:BaseUrl"];
    var externalHost = builder.Configuration["ExternalApi:Host"];
    client.BaseAddress = new Uri(externalBaseUrl ?? (externalHost is null ? "http://localhost:5081" : $"http://{externalHost}"));
    // La API externa también usa el plan gratuito de Render y puede necesitar
    // cerca de un minuto para reactivarse después de un período de inactividad.
    client.Timeout = TimeSpan.FromSeconds(75);
});

var app = builder.Build();
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/error", () => Results.Problem("Ocurrió un error inesperado."));
app.MapControllerRoute(name: "default", pattern: "{controller=Dashboard}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
    await DbSeeder.SeedIdentityAsync(scope.ServiceProvider, builder.Configuration, app.Environment);
}

app.Run();
