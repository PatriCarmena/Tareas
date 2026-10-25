using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using OrderDesk.Web.Models;

namespace OrderDesk.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<WebhookReceipt> WebhookReceipts => Set<WebhookReceipt>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();
    public DbSet<CustomerActivity> CustomerActivities => Set<CustomerActivity>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<OpportunityWebhookReceipt> OpportunityWebhookReceipts => Set<OpportunityWebhookReceipt>();

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
        modelBuilder.Entity<CustomerActivity>().Property(x => x.Type).HasConversion<string>();
        modelBuilder.Entity<CustomerActivity>().Property(x => x.Priority).HasConversion<string>();
        modelBuilder.Entity<CustomerActivity>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<CustomerActivity>().HasIndex(x => new { x.AssignedUserId, x.Status, x.ScheduledAtUtc });
        modelBuilder.Entity<CustomerActivity>().HasIndex(x => new { x.CustomerId, x.ScheduledAtUtc });
        modelBuilder.Entity<CustomerActivity>().HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<CustomerActivity>().HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Opportunity>().Property(x => x.Stage).HasConversion<string>();
        modelBuilder.Entity<Opportunity>().Property(x => x.EstimatedAmount).HasPrecision(12, 2);
        modelBuilder.Entity<Opportunity>().HasIndex(x => new { x.Stage, x.ExpectedCloseDate });
        modelBuilder.Entity<Opportunity>().HasIndex(x => x.CustomerId);
        modelBuilder.Entity<Opportunity>().HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Order>().HasOne(x => x.Opportunity).WithOne(x => x.ConvertedOrder).HasForeignKey<Order>(x => x.OpportunityId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Order>().HasIndex(x => x.OpportunityId).IsUnique();
        modelBuilder.Entity<Order>().HasOne(x => x.Salesperson).WithMany().HasForeignKey(x => x.SalespersonId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<OpportunityWebhookReceipt>().HasIndex(x => x.EventId).IsUnique();
        modelBuilder.Entity<OpportunityWebhookReceipt>().HasOne(x => x.Opportunity).WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Cascade);
    }
}
