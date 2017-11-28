using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace KiteBotCore.Migrations
{
    public partial class addDateTimeForLastCheckedPremium : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.RenameColumn(
            //    name: "Id",
            //    table: "UserColorRoles",
            //    newName: "RoleId");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PremiumLastCheckedAt",
                table: "Users",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PremiumLastCheckedAt",
                table: "Users");

            //migrationBuilder.RenameColumn(
            //    name: "RoleId",
            //    table: "UserColorRoles",
            //    newName: "Id");
        }
    }
}
