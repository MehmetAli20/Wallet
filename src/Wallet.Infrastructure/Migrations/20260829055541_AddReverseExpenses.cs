using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReverseExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_IdempotencyRecords",
                table: "IdempotencyRecords");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "IdempotencyRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacesExpenseId",
                table: "Expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                table: "Expenses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReversedAt",
                table: "Expenses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversedBy",
                table: "Expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_IdempotencyRecords",
                table: "IdempotencyRecords",
                columns: new[] { "UserId", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ReplacesExpenseId",
                table: "Expenses",
                column: "ReplacesExpenseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Expenses_ReplacesExpenseId",
                table: "Expenses",
                column: "ReplacesExpenseId",
                principalTable: "Expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Expenses_ReplacesExpenseId",
                table: "Expenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IdempotencyRecords",
                table: "IdempotencyRecords");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ReplacesExpenseId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "IdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "ReplacesExpenseId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ReversalReason",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ReversedAt",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ReversedBy",
                table: "Expenses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IdempotencyRecords",
                table: "IdempotencyRecords",
                column: "Key");
        }
    }
}
