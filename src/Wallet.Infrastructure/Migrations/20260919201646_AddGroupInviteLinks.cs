using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupInviteLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Token",
                table: "PlaceholderClaims",
                newName: "TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_PlaceholderClaims_Token",
                table: "PlaceholderClaims",
                newName: "IX_PlaceholderClaims_TokenHash");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAt",
                table: "PlaceholderClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GroupInviteLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MaxUses = table.Column<int>(type: "integer", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupInviteLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupInviteLinks_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupInviteLinkRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupInviteLinkRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupInviteLinkRedemptions_GroupInviteLinks_LinkId",
                        column: x => x.LinkId,
                        principalTable: "GroupInviteLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupInviteLinkRedemptions_LinkId_UserId",
                table: "GroupInviteLinkRedemptions",
                columns: new[] { "LinkId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupInviteLinks_GroupId",
                table: "GroupInviteLinks",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupInviteLinks_TokenHash",
                table: "GroupInviteLinks",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupInviteLinkRedemptions");

            migrationBuilder.DropTable(
                name: "GroupInviteLinks");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "PlaceholderClaims");

            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "PlaceholderClaims",
                newName: "Token");

            migrationBuilder.RenameIndex(
                name: "IX_PlaceholderClaims_TokenHash",
                table: "PlaceholderClaims",
                newName: "IX_PlaceholderClaims_Token");
        }
    }
}
