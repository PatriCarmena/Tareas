using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderDesk.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261025010000_LimitPasswordResetAttempts")]
public sealed class LimitPasswordResetAttempts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("FailedAttempts", "PasswordResetCodes", "integer", nullable: false, defaultValue: 0);
        // Los códigos existentes seguirán funcionando con un token generado
        // después de verificarlos; ya no hace falta conservar el token anterior.
        migrationBuilder.Sql("UPDATE \"PasswordResetCodes\" SET \"ResetToken\" = '' WHERE \"ResetToken\" <> '';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("FailedAttempts", "PasswordResetCodes");
    }
}
