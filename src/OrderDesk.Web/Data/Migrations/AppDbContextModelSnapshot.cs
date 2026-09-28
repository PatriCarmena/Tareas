using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OrderDesk.Web.Data;

#nullable disable
namespace OrderDesk.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion","10.0.0").HasAnnotation("Relational:MaxIdentifierLength",63);
        modelBuilder.Entity("OrderDesk.Web.Models.Customer",b=>{b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").HasAnnotation("Npgsql:ValueGenerationStrategy",NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);b.Property<string>("Address").HasMaxLength(200).HasColumnType("character varying(200)");b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");b.Property<string>("Email").IsRequired().HasMaxLength(180).HasColumnType("character varying(180)");b.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");b.Property<string>("Phone").HasMaxLength(40).HasColumnType("character varying(40)");b.HasKey("Id");b.HasIndex("Email").IsUnique();b.ToTable("Customers");});
        modelBuilder.Entity("OrderDesk.Web.Models.Order",b=>{b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").HasAnnotation("Npgsql:ValueGenerationStrategy",NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");b.Property<int>("CustomerId").HasColumnType("integer");b.Property<string>("Description").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");b.Property<string>("Number").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");b.Property<string>("Status").IsRequired().HasColumnType("text");b.Property<decimal>("Total").HasColumnType("numeric(12,2)");b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");b.HasKey("Id");b.HasIndex("CustomerId");b.HasIndex("Number").IsUnique();b.ToTable("Orders");});
        modelBuilder.Entity("OrderDesk.Web.Models.WebhookReceipt",b=>{b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").HasAnnotation("Npgsql:ValueGenerationStrategy",NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);b.Property<string>("EventId").IsRequired().HasColumnType("text");b.Property<int>("OrderId").HasColumnType("integer");b.Property<DateTime>("ProcessedAtUtc").HasColumnType("timestamp with time zone");b.HasKey("Id");b.HasIndex("EventId").IsUnique();b.HasIndex("OrderId");b.ToTable("WebhookReceipts");});
        modelBuilder.Entity("OrderDesk.Web.Models.Order",b=>{b.HasOne("OrderDesk.Web.Models.Customer","Customer").WithMany("Orders").HasForeignKey("CustomerId").OnDelete(DeleteBehavior.Cascade).IsRequired();b.Navigation("Customer");});
        modelBuilder.Entity("OrderDesk.Web.Models.WebhookReceipt",b=>{b.HasOne("OrderDesk.Web.Models.Order","Order").WithMany("WebhookReceipts").HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade).IsRequired();b.Navigation("Order");});
        modelBuilder.Entity("OrderDesk.Web.Models.Customer",b=>b.Navigation("Orders"));modelBuilder.Entity("OrderDesk.Web.Models.Order",b=>b.Navigation("WebhookReceipts"));
    }
}
