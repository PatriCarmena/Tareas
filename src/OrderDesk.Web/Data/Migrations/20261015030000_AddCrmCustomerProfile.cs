using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderDesk.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261015030000_AddCrmCustomerProfile")]
public sealed class AddCrmCustomerProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("AdditionalInfo", "Customers", "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>("AssignedUserId", "Customers", "text", nullable: true);
        migrationBuilder.AddColumn<string>("Company", "Customers", "character varying(160)", maxLength: 160, nullable: true);
        migrationBuilder.AddColumn<DateTime>("LastContactAtUtc", "Customers", "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>("Position", "Customers", "character varying(120)", maxLength: 120, nullable: true);
        migrationBuilder.AddColumn<string>("Tags", "Customers", "character varying(250)", maxLength: 250, nullable: true);

        migrationBuilder.CreateTable(
            name: "CustomerNotes",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CustomerId = table.Column<int>(type: "integer", nullable: false),
                Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerNotes", x => x.Id);
                table.ForeignKey("FK_CustomerNotes_AspNetUsers_CreatedByUserId", x => x.CreatedByUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_CustomerNotes_Customers_CustomerId", x => x.CustomerId, "Customers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Customers_AssignedUserId", "Customers", "AssignedUserId");
        migrationBuilder.CreateIndex("IX_CustomerNotes_CreatedByUserId", "CustomerNotes", "CreatedByUserId");
        migrationBuilder.CreateIndex("IX_CustomerNotes_CustomerId_CreatedAtUtc", "CustomerNotes", new[] { "CustomerId", "CreatedAtUtc" });
        migrationBuilder.AddForeignKey("FK_Customers_AspNetUsers_AssignedUserId", "Customers", "AssignedUserId", "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("CustomerNotes");
        migrationBuilder.DropForeignKey("FK_Customers_AspNetUsers_AssignedUserId", "Customers");
        migrationBuilder.DropIndex("IX_Customers_AssignedUserId", "Customers");
        migrationBuilder.DropColumn("AdditionalInfo", "Customers");
        migrationBuilder.DropColumn("AssignedUserId", "Customers");
        migrationBuilder.DropColumn("Company", "Customers");
        migrationBuilder.DropColumn("LastContactAtUtc", "Customers");
        migrationBuilder.DropColumn("Position", "Customers");
        migrationBuilder.DropColumn("Tags", "Customers");
    }
}
