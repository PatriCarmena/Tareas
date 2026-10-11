using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace OrderDesk.Web.Data;

public static class DbSeeder
{
    public static async Task SeedIdentityAsync(IServiceProvider services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
            if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role));

        var email = configuration["SeedAdmin:Email"] ?? "admin@nexo.local";
        var password = configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(password) && environment.IsDevelopment()) password = "NexoAdmin!2026";
        if (string.IsNullOrWhiteSpace(password)) return;

        var users = services.GetRequiredService<UserManager<AppUser>>();
        var admin = await users.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new AppUser { UserName = email, Email = email, FullName = "Administrador Nexo", EmailConfirmed = true };
            var result = await users.CreateAsync(admin, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
        if (!await users.IsInRoleAsync(admin, AppRoles.Administrator)) await users.AddToRoleAsync(admin, AppRoles.Administrator);
    }

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Customers.AnyAsync()) return;
        var customers = new[]
        {
            new Customer { Name = "Ana Martínez", Email = "ana@ejemplo.com", Phone = "+54 11 4321-0001", Address = "Palermo, CABA" },
            new Customer { Name = "Estudio Norte", Email = "compras@estudionorte.com", Phone = "+54 11 4321-0002", Address = "Vicente López, Buenos Aires" },
            new Customer { Name = "Carlos Ríos", Email = "carlos@ejemplo.com", Phone = "+54 341 555-0190", Address = "Rosario, Santa Fe" }
        };
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();
        db.Orders.AddRange(
            new Order { CustomerId = customers[0].Id, Number = "PED-1001", Description = "Licencias anuales", Total = 128000, Status = OrderStatus.InProgress },
            new Order { CustomerId = customers[0].Id, Number = "PED-1002", Description = "Servicio de implementación", Total = 86000, Status = OrderStatus.Pending },
            new Order { CustomerId = customers[1].Id, Number = "PED-1003", Description = "Equipamiento de oficina", Total = 245500, Status = OrderStatus.Shipped });
        await db.SaveChangesAsync();
    }
}
