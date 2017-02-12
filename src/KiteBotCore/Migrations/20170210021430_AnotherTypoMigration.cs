using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KiteBotCore.Migrations
{
    public partial class AnotherTypoMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Channels_ChannelForeignKey",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Users_UserForeignKey",
                table: "Posts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Posts",
                table: "Posts");

            migrationBuilder.RenameTable(
                name: "Posts",
                newName: "Messages");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_UserForeignKey",
                table: "Messages",
                newName: "IX_Messages_UserForeignKey");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_ChannelForeignKey",
                table: "Messages",
                newName: "IX_Messages_ChannelForeignKey");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                table: "Messages",
                column: "MessageId");

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Channels_ChannelForeignKey",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserForeignKey",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                table: "Messages");

            migrationBuilder.RenameTable(
                name: "Messages",
                newName: "Posts");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_UserForeignKey",
                table: "Posts",
                newName: "IX_Posts_UserForeignKey");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_ChannelForeignKey",
                table: "Posts",
                newName: "IX_Posts_ChannelForeignKey");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Posts",
                table: "Posts",
                column: "MessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Channels_ChannelForeignKey",
                table: "Posts",
                column: "ChannelForeignKey",
                principalTable: "Channels",
                principalColumn: "ChannelId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Users_UserForeignKey",
                table: "Posts",
                column: "UserForeignKey",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
