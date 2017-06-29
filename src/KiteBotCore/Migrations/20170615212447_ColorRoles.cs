using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KiteBotCore.Migrations
{
    public partial class ColorRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ColorRole",
                columns: table => new
                {
                    ColorRoleId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    RemovalAt = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColorRole", x => x.ColorRoleId);
                });

            migrationBuilder.CreateTable(
                name: "UserColorRoles",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false),
                    RoleId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserColorRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserColorRoles_ColorRole_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ColorRole",
                        principalColumn: "ColorRoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserColorRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserColorRoles_RoleId",
                table: "UserColorRoles",
                column: "RoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserColorRoles");

            migrationBuilder.DropTable(
                name: "ColorRole");
        }
    }
}
