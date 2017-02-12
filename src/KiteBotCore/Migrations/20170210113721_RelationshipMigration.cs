using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KiteBotCore.Migrations
{
    public partial class RelationshipMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channels_Guilds_GuildForeignKey",
                table: "Channels");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Channels_ChannelForeignKey",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserForeignKey",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Guilds_GuildForeignKey",
                table: "Users");

            migrationBuilder.AlterColumn<long>(
                name: "GuildForeignKey",
                table: "Users",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "UserForeignKey",
                table: "Messages",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ChannelForeignKey",
                table: "Messages",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "GuildForeignKey",
                table: "Channels",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Channels_Guilds_GuildForeignKey",
                table: "Channels",
                column: "GuildForeignKey",
                principalTable: "Guilds",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Channels_ChannelForeignKey",
                table: "Messages",
                column: "ChannelForeignKey",
                principalTable: "Channels",
                principalColumn: "ChannelId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserForeignKey",
                table: "Messages",
                column: "UserForeignKey",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Guilds_GuildForeignKey",
                table: "Users",
                column: "GuildForeignKey",
                principalTable: "Guilds",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channels_Guilds_GuildForeignKey",
                table: "Channels");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Channels_ChannelForeignKey",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserForeignKey",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Guilds_GuildForeignKey",
                table: "Users");

            migrationBuilder.AlterColumn<long>(
                name: "GuildForeignKey",
                table: "Users",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<long>(
                name: "UserForeignKey",
                table: "Messages",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<long>(
                name: "ChannelForeignKey",
                table: "Messages",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<long>(
                name: "GuildForeignKey",
                table: "Channels",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddForeignKey(
                name: "FK_Channels_Guilds_GuildForeignKey",
                table: "Channels",
                column: "GuildForeignKey",
                principalTable: "Guilds",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Channels_ChannelForeignKey",
                table: "Messages",
                column: "ChannelForeignKey",
                principalTable: "Channels",
                principalColumn: "ChannelId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserForeignKey",
                table: "Messages",
                column: "UserForeignKey",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Guilds_GuildForeignKey",
                table: "Users",
                column: "GuildForeignKey",
                principalTable: "Guilds",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
