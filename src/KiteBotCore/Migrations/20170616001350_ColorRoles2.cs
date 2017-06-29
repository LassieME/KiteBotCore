using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KiteBotCore.Migrations
{
    public partial class ColorRoles2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserColorRoles_ColorRole_RoleId",
                table: "UserColorRoles");

            migrationBuilder.DropTable(
                name: "ColorRole");

            migrationBuilder.DropIndex(
                name: "IX_UserColorRoles_RoleId",
                table: "UserColorRoles");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RemovalAt",
                table: "UserColorRoles",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemovalAt",
                table: "UserColorRoles");

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

            migrationBuilder.CreateIndex(
                name: "IX_UserColorRoles_RoleId",
                table: "UserColorRoles",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserColorRoles_ColorRole_RoleId",
                table: "UserColorRoles",
                column: "RoleId",
                principalTable: "ColorRole",
                principalColumn: "ColorRoleId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
