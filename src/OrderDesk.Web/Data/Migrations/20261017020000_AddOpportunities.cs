using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderDesk.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261017020000_AddOpportunities")]
public sealed class AddOpportunities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Opportunities",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CustomerId = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                EstimatedAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                Probability = table.Column<int>(type: "integer", nullable: false),
                ExpectedCloseDate = table.Column<DateOnly>(type: "date", nullable: false),
                Stage = table.Column<string>(type: "text", nullable: false),
                AssignedUserId = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Opportunities", x => x.Id);
                table.ForeignKey("FK_Opportunities_Customers_CustomerId", x => x.CustomerId, "Customers", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Opportunities_AspNetUsers_AssignedUserId", x => x.AssignedUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_Opportunities_CustomerId", "Opportunities", "CustomerId");
        migrationBuilder.CreateIndex("IX_Opportunities_AssignedUserId", "Opportunities", "AssignedUserId");
        migrationBuilder.CreateIndex("IX_Opportunities_Stage_ExpectedCloseDate", "Opportunities", new[] { "Stage", "ExpectedCloseDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("Opportunities");
}
