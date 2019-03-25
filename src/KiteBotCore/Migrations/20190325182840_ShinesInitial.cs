using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace KiteBotCore.Migrations
{
    public partial class ShinesInitial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OptOut",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Premium",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PremiumLastCheckedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RegToken",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "Shines",
                table: "Users",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ShineBetEvent",
                columns: table => new
                {
                    ShineBetEventId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CreationDateTime = table.Column<DateTimeOffset>(nullable: false),
                    TimeUntilClose = table.Column<TimeSpan>(nullable: false),
                    Question = table.Column<string>(nullable: false),
                    BetAmount = table.Column<int>(nullable: false),
                    BetResult = table.Column<bool>(nullable: true),
                    OwnerUserUserId = table.Column<long>(nullable: false),
                    ChannelId = table.Column<long>(nullable: false),
                    GuildId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShineBetEvent", x => x.ShineBetEventId);
                    table.ForeignKey(
                        name: "FK_ShineBetEvent_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "ChannelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShineBetEvent_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShineBetEvent_Users_OwnerUserUserId",
                        column: x => x.OwnerUserUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShineBet",
                columns: table => new
                {
                    ShineBetId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Answer = table.Column<bool>(nullable: false),
                    DoubleDown = table.Column<bool>(nullable: false),
                    UserId = table.Column<long>(nullable: true),
                    ShineBetEventId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShineBet", x => x.ShineBetId);
                    table.ForeignKey(
                        name: "FK_ShineBet_ShineBetEvent_ShineBetEventId",
                        column: x => x.ShineBetEventId,
                        principalTable: "ShineBetEvent",
                        principalColumn: "ShineBetEventId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShineBet_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShineBet_ShineBetEventId",
                table: "ShineBet",
                column: "ShineBetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ShineBet_UserId",
                table: "ShineBet",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShineBetEvent_ChannelId",
                table: "ShineBetEvent",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ShineBetEvent_GuildId",
                table: "ShineBetEvent",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ShineBetEvent_OwnerUserUserId",
                table: "ShineBetEvent",
                column: "OwnerUserUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShineBet");

            migrationBuilder.DropTable(
                name: "ShineBetEvent");

            migrationBuilder.DropColumn(
                name: "Shines",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "OptOut",
                table: "Users",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Premium",
                table: "Users",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PremiumLastCheckedAt",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegToken",
                table: "Users",
                nullable: true);
        }
    }
}
