using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using OrderDesk.Web.Models;

namespace OrderDesk.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<WebhookReceipt> WebhookReceipts => Set<WebhookReceipt>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Customer>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(x => x.Number).IsUnique();
        modelBuilder.Entity<WebhookReceipt>().HasIndex(x => x.EventId).IsUnique();
        modelBuilder.Entity<Order>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<OrderStatusHistory>().Property(x => x.PreviousStatus).HasConversion<string>();
        modelBuilder.Entity<OrderStatusHistory>().Property(x => x.NewStatus).HasConversion<string>();
        modelBuilder.Entity<OrderStatusHistory>().HasIndex(x => new { x.OrderId, x.ChangedAtUtc });
        modelBuilder.Entity<PasswordResetCode>().HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
        modelBuilder.Entity<Customer>().HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<CustomerNote>().HasIndex(x => new { x.CustomerId, x.CreatedAtUtc });
        modelBuilder.Entity<CustomerNote>().HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
