using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderDesk.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261017010000_AddCustomerActivities")]
public sealed class AddCustomerActivities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CustomerActivities",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CustomerId = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Type = table.Column<string>(type: "text", nullable: false),
                Priority = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                ScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AssignedUserId = table.Column<string>(type: "text", nullable: true),
                CreatedByUserId = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerActivities", x => x.Id);
                table.ForeignKey("FK_CustomerActivities_Customers_CustomerId", x => x.CustomerId, "Customers", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_CustomerActivities_AspNetUsers_AssignedUserId", x => x.AssignedUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_CustomerActivities_AspNetUsers_CreatedByUserId", x => x.CreatedByUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_CustomerActivities_CustomerId_ScheduledAtUtc", "CustomerActivities", new[] { "CustomerId", "ScheduledAtUtc" });
        migrationBuilder.CreateIndex("IX_CustomerActivities_AssignedUserId_Status_ScheduledAtUtc", "CustomerActivities", new[] { "AssignedUserId", "Status", "ScheduledAtUtc" });
        migrationBuilder.CreateIndex("IX_CustomerActivities_CreatedByUserId", "CustomerActivities", "CreatedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("CustomerActivities");
    }
}
