using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Models;

namespace OrderDesk.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<WebhookReceipt> WebhookReceipts => Set<WebhookReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(x => x.Number).IsUnique();
        modelBuilder.Entity<WebhookReceipt>().HasIndex(x => x.EventId).IsUnique();
        modelBuilder.Entity<Order>().Property(x => x.Status).HasConversion<string>();
    }
}
