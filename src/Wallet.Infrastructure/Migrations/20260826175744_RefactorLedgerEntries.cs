using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorLedgerEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CounterpartyId",
                table: "LedgerEntries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_GroupId_OwnerId_CounterpartyId",
                table: "LedgerEntries",
                columns: new[] { "GroupId", "OwnerId", "CounterpartyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_GroupId_OwnerId_CounterpartyId",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "CounterpartyId",
                table: "LedgerEntries");
        }
    }
}
