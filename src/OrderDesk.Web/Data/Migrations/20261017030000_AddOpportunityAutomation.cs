using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderDesk.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261017030000_AddOpportunityAutomation")]
public sealed class AddOpportunityAutomation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "OpportunityId", table: "Orders", type: "integer", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Orders_OpportunityId", table: "Orders", column: "OpportunityId", unique: true);
        migrationBuilder.AddForeignKey(name: "FK_Orders_Opportunities_OpportunityId", table: "Orders", column: "OpportunityId",
            principalTable: "Opportunities", principalColumn: "Id", onDelete: ReferentialAction.SetNull);

        migrationBuilder.CreateTable(name: "OpportunityWebhookReceipts", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            EventId = table.Column<string>(type: "text", nullable: false),
            OpportunityId = table.Column<int>(type: "integer", nullable: false),
            ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_OpportunityWebhookReceipts", x => x.Id);
            table.ForeignKey("FK_OpportunityWebhookReceipts_Opportunities_OpportunityId", x => x.OpportunityId,
                "Opportunities", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_OpportunityWebhookReceipts_EventId", "OpportunityWebhookReceipts", "EventId", unique: true);
        migrationBuilder.CreateIndex("IX_OpportunityWebhookReceipts_OpportunityId", "OpportunityWebhookReceipts", "OpportunityId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("OpportunityWebhookReceipts");
        migrationBuilder.DropForeignKey("FK_Orders_Opportunities_OpportunityId", "Orders");
        migrationBuilder.DropIndex("IX_Orders_OpportunityId", "Orders");
        migrationBuilder.DropColumn("OpportunityId", "Orders");
    }
}
