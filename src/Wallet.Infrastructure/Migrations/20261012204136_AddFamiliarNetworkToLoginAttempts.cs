using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFamiliarNetworkToLoginAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FromFamiliarNetwork",
                table: "LoginAttempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserId_FromFamiliarNetwork_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "UserId", "FromFamiliarNetwork", "AttemptedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoginAttempts_UserId_FromFamiliarNetwork_AttemptedAt",
                table: "LoginAttempts");

            migrationBuilder.DropColumn(
                name: "FromFamiliarNetwork",
                table: "LoginAttempts");
        }
    }
}
