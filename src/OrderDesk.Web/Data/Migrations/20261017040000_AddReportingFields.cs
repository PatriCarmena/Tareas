using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderDesk.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261017040000_AddReportingFields")]
public sealed class AddReportingFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("ClosedAtUtc", "Opportunities", "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>("LostReason", "Opportunities", "character varying(250)", maxLength: 250, nullable: true);
        migrationBuilder.AddColumn<string>("SalespersonId", "Orders", "text", nullable: true);
        migrationBuilder.AddColumn<string>("SalespersonName", "Orders", "character varying(120)", maxLength: 120, nullable: true);
        migrationBuilder.CreateIndex("IX_Orders_SalespersonId", "Orders", "SalespersonId");
        migrationBuilder.AddForeignKey("FK_Orders_AspNetUsers_SalespersonId", "Orders", "SalespersonId", "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        migrationBuilder.Sql("""
            UPDATE "Orders" AS o
            SET "SalespersonId" = COALESCE(
                (SELECT p."AssignedUserId" FROM "Opportunities" AS p WHERE p."Id" = o."OpportunityId"),
                (SELECT c."AssignedUserId" FROM "Customers" AS c WHERE c."Id" = o."CustomerId"));
            UPDATE "Orders" AS o
            SET "SalespersonName" = (SELECT u."FullName" FROM "AspNetUsers" AS u WHERE u."Id" = o."SalespersonId")
            WHERE o."SalespersonId" IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Orders_AspNetUsers_SalespersonId", "Orders");
        migrationBuilder.DropIndex("IX_Orders_SalespersonId", "Orders");
        migrationBuilder.DropColumn("SalespersonId", "Orders");
        migrationBuilder.DropColumn("SalespersonName", "Orders");
        migrationBuilder.DropColumn("ClosedAtUtc", "Opportunities");
        migrationBuilder.DropColumn("LostReason", "Opportunities");
    }
}
