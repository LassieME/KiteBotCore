using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace KiteBotCore.Migrations
{
    public partial class GuildShineRemoval : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShineBet_ShineBetEvent_ShineBetEventId",
                table: "ShineBet");

            migrationBuilder.DropForeignKey(
                name: "FK_ShineBet_Users_UserId",
                table: "ShineBet");

            migrationBuilder.DropForeignKey(
                name: "FK_ShineBetEvent_Channels_ChannelId",
                table: "ShineBetEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_ShineBetEvent_Guilds_GuildId",
                table: "ShineBetEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_ShineBetEvent_Users_OwnerUserUserId",
                table: "ShineBetEvent");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShineBetEvent",
                table: "ShineBetEvent");

            migrationBuilder.DropIndex(
                name: "IX_ShineBetEvent_GuildId",
                table: "ShineBetEvent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShineBet",
                table: "ShineBet");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "ShineBetEvent");

            migrationBuilder.RenameTable(
                name: "ShineBetEvent",
                newName: "ShineBetEvents");

            migrationBuilder.RenameTable(
                name: "ShineBet",
                newName: "ShineBets");

            migrationBuilder.RenameIndex(
                name: "IX_ShineBetEvent_OwnerUserUserId",
                table: "ShineBetEvents",
                newName: "IX_ShineBetEvents_OwnerUserUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ShineBetEvent_ChannelId",
                table: "ShineBetEvents",
                newName: "IX_ShineBetEvents_ChannelId");

            migrationBuilder.RenameIndex(
                name: "IX_ShineBet_UserId",
                table: "ShineBets",
                newName: "IX_ShineBets_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ShineBet_ShineBetEventId",
                table: "ShineBets",
                newName: "IX_ShineBets_ShineBetEventId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShineBetEvents",
                table: "ShineBetEvents",
                column: "ShineBetEventId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShineBets",
                table: "ShineBets",
                column: "ShineBetId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShineBetEvents_Channels_ChannelId",
                table: "ShineBetEvents",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "ChannelId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShineBetEvents_Users_OwnerUserUserId",
                table: "ShineBetEvents",
                column: "OwnerUserUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShineBets_ShineBetEvents_ShineBetEventId",
                table: "ShineBets",
                column: "ShineBetEventId",
                principalTable: "ShineBetEvents",
                principalColumn: "ShineBetEventId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShineBets_Users_UserId",
                table: "ShineBets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShineBetEvents_Channels_ChannelId",
                table: "ShineBetEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ShineBetEvents_Users_OwnerUserUserId",
                table: "ShineBetEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ShineBets_ShineBetEvents_ShineBetEventId",
                table: "ShineBets");

            migrationBuilder.DropForeignKey(
                name: "FK_ShineBets_Users_UserId",
                table: "ShineBets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShineBets",
                table: "ShineBets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShineBetEvents",
                table: "ShineBetEvents");

            migrationBuilder.RenameTable(
                name: "ShineBets",
                newName: "ShineBet");

            migrationBuilder.RenameTable(
                name: "ShineBetEvents",
                newName: "ShineBetEvent");

            migrationBuilder.RenameIndex(
                name: "IX_ShineBets_UserId",
                table: "ShineBet",
                newName: "IX_ShineBet_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ShineBets_ShineBetEventId",
                table: "ShineBet",
                newName: "IX_ShineBet_ShineBetEventId");

            migrationBuilder.RenameIndex(
                name: "IX_ShineBetEvents_OwnerUserUserId",
                table: "ShineBetEvent",
                newName: "IX_ShineBetEvent_OwnerUserUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ShineBetEvents_ChannelId",
                table: "ShineBetEvent",
                newName: "IX_ShineBetEvent_ChannelId");

            migrationBuilder.AddColumn<long>(
                name: "GuildId",
                table: "ShineBetEvent",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShineBet",
                table: "ShineBet",
                column: "ShineBetId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShineBetEvent",
                table: "ShineBetEvent",
                column: "ShineBetEventId");

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateTime = table.Column<DateTimeOffset>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    GuildId = table.Column<long>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    UserId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_Events_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Events_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShineBetEvent_GuildId",
                table: "ShineBetEvent",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_GuildId",
                table: "Events",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_UserId",
                table: "Events",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShineBet_ShineBetEvent_ShineBetEventId",
                table: "ShineBet",
                column: "ShineBetEventId",
                principalTable: "ShineBetEvent",
                principalColumn: "ShineBetEventId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShineBet_Users_UserId",
                table: "ShineBet",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShineBetEvent_Channels_ChannelId",
                table: "ShineBetEvent",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "ChannelId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShineBetEvent_Guilds_GuildId",
                table: "ShineBetEvent",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShineBetEvent_Users_OwnerUserUserId",
                table: "ShineBetEvent",
                column: "OwnerUserUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
