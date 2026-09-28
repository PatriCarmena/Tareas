using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OrderDesk.Web.Data;

#nullable disable
namespace OrderDesk.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609280001_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name:"Customers",columns:table=>new{Id=table.Column<int>(type:"integer",nullable:false).Annotation("Npgsql:ValueGenerationStrategy",NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),Name=table.Column<string>(type:"character varying(120)",maxLength:120,nullable:false),Email=table.Column<string>(type:"character varying(180)",maxLength:180,nullable:false),Phone=table.Column<string>(type:"character varying(40)",maxLength:40,nullable:true),Address=table.Column<string>(type:"character varying(200)",maxLength:200,nullable:true),CreatedAtUtc=table.Column<DateTime>(type:"timestamp with time zone",nullable:false)},constraints:table=>table.PrimaryKey("PK_Customers",x=>x.Id));
        migrationBuilder.CreateTable(name:"Orders",columns:table=>new{Id=table.Column<int>(type:"integer",nullable:false).Annotation("Npgsql:ValueGenerationStrategy",NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),CustomerId=table.Column<int>(type:"integer",nullable:false),Number=table.Column<string>(type:"character varying(80)",maxLength:80,nullable:false),Description=table.Column<string>(type:"character varying(500)",maxLength:500,nullable:false),Total=table.Column<decimal>(type:"numeric(12,2)",nullable:false),Status=table.Column<string>(type:"text",nullable:false),CreatedAtUtc=table.Column<DateTime>(type:"timestamp with time zone",nullable:false),UpdatedAtUtc=table.Column<DateTime>(type:"timestamp with time zone",nullable:false)},constraints:table=>{table.PrimaryKey("PK_Orders",x=>x.Id);table.ForeignKey(name:"FK_Orders_Customers_CustomerId",column:x=>x.CustomerId,principalTable:"Customers",principalColumn:"Id",onDelete:ReferentialAction.Cascade);});
        migrationBuilder.CreateTable(name:"WebhookReceipts",columns:table=>new{Id=table.Column<int>(type:"integer",nullable:false).Annotation("Npgsql:ValueGenerationStrategy",NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),EventId=table.Column<string>(type:"text",nullable:false),OrderId=table.Column<int>(type:"integer",nullable:false),ProcessedAtUtc=table.Column<DateTime>(type:"timestamp with time zone",nullable:false)},constraints:table=>{table.PrimaryKey("PK_WebhookReceipts",x=>x.Id);table.ForeignKey(name:"FK_WebhookReceipts_Orders_OrderId",column:x=>x.OrderId,principalTable:"Orders",principalColumn:"Id",onDelete:ReferentialAction.Cascade);});
        migrationBuilder.CreateIndex(name:"IX_Customers_Email",table:"Customers",column:"Email",unique:true);migrationBuilder.CreateIndex(name:"IX_Orders_CustomerId",table:"Orders",column:"CustomerId");migrationBuilder.CreateIndex(name:"IX_Orders_Number",table:"Orders",column:"Number",unique:true);migrationBuilder.CreateIndex(name:"IX_WebhookReceipts_EventId",table:"WebhookReceipts",column:"EventId",unique:true);migrationBuilder.CreateIndex(name:"IX_WebhookReceipts_OrderId",table:"WebhookReceipts",column:"OrderId");
    }
    protected override void Down(MigrationBuilder migrationBuilder){migrationBuilder.DropTable("WebhookReceipts");migrationBuilder.DropTable("Orders");migrationBuilder.DropTable("Customers");}
}
