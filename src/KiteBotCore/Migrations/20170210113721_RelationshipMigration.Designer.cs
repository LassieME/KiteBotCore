using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using KiteBotCore;

namespace KiteBotCore.Migrations
{
    [DbContext(typeof(DiscordContext))]
    [Migration("20170210113721_RelationshipMigration")]
    partial class RelationshipMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752");

            modelBuilder.Entity("KiteBotCore.Channel", b =>
                {
                    b.Property<long>("ChannelId")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("GuildForeignKey")
                        .IsRequired();

                    b.Property<string>("Name");

                    b.HasKey("ChannelId");

                    b.HasIndex("GuildForeignKey");

                    b.ToTable("Channels");
                });

            modelBuilder.Entity("KiteBotCore.Guild", b =>
                {
                    b.Property<long>("GuildId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("GuildId");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("KiteBotCore.Message", b =>
                {
                    b.Property<long>("MessageId")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("ChannelForeignKey")
                        .IsRequired();

                    b.Property<string>("Content");

                    b.Property<long?>("UserForeignKey")
                        .IsRequired();

                    b.HasKey("MessageId");

                    b.HasIndex("ChannelForeignKey");

                    b.HasIndex("UserForeignKey");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("KiteBotCore.User", b =>
                {
                    b.Property<long>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("GuildForeignKey")
                        .IsRequired();

                    b.Property<DateTimeOffset?>("JoinedAt");

                    b.Property<DateTimeOffset>("LastActivityAt");

                    b.Property<string>("Name");

                    b.HasKey("UserId");

                    b.HasIndex("GuildForeignKey");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("KiteBotCore.Channel", b =>
                {
                    b.HasOne("KiteBotCore.Guild", "Guild")
                        .WithMany("Channels")
                        .HasForeignKey("GuildForeignKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("KiteBotCore.Message", b =>
                {
                    b.HasOne("KiteBotCore.Channel", "Channel")
                        .WithMany("Messages")
                        .HasForeignKey("ChannelForeignKey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("KiteBotCore.User", "User")
                        .WithMany("Messages")
                        .HasForeignKey("UserForeignKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("KiteBotCore.User", b =>
                {
                    b.HasOne("KiteBotCore.Guild", "Guild")
                        .WithMany("Users")
                        .HasForeignKey("GuildForeignKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
