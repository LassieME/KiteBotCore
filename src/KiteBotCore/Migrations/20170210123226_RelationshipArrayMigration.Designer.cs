using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using KiteBotCore;

namespace KiteBotCore.Migrations
{
    [DbContext(typeof(DiscordContext))]
    [Migration("20170210123226_RelationshipArrayMigration")]
    partial class RelationshipArrayMigration
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

                    b.Property<long?>("GuildId");

                    b.Property<string>("Name");

                    b.HasKey("ChannelId");

                    b.HasIndex("GuildForeignKey");

                    b.HasIndex("GuildId");

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

                    b.Property<long?>("ChannelId");

                    b.Property<string>("Content");

                    b.Property<long?>("UserForeignKey")
                        .IsRequired();

                    b.Property<long?>("UserId");

                    b.HasKey("MessageId");

                    b.HasIndex("ChannelForeignKey");

                    b.HasIndex("ChannelId");

                    b.HasIndex("UserForeignKey");

                    b.HasIndex("UserId");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("KiteBotCore.User", b =>
                {
                    b.Property<long>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("GuildForeignKey")
                        .IsRequired();

                    b.Property<long?>("GuildId");

                    b.Property<DateTimeOffset?>("JoinedAt");

                    b.Property<DateTimeOffset>("LastActivityAt");

                    b.Property<string>("Name");

                    b.HasKey("UserId");

                    b.HasIndex("GuildForeignKey");

                    b.HasIndex("GuildId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("KiteBotCore.Channel", b =>
                {
                    b.HasOne("KiteBotCore.Guild", "Guild")
                        .WithMany("Channels")
                        .HasForeignKey("GuildForeignKey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("KiteBotCore.Guild")
                        .WithMany("ChannelsArray")
                        .HasForeignKey("GuildId");
                });

            modelBuilder.Entity("KiteBotCore.Message", b =>
                {
                    b.HasOne("KiteBotCore.Channel", "Channel")
                        .WithMany("Messages")
                        .HasForeignKey("ChannelForeignKey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("KiteBotCore.Channel")
                        .WithMany("MessagesArray")
                        .HasForeignKey("ChannelId");

                    b.HasOne("KiteBotCore.User", "User")
                        .WithMany("Messages")
                        .HasForeignKey("UserForeignKey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("KiteBotCore.User")
                        .WithMany("MessagesArray")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("KiteBotCore.User", b =>
                {
                    b.HasOne("KiteBotCore.Guild", "Guild")
                        .WithMany("Users")
                        .HasForeignKey("GuildForeignKey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("KiteBotCore.Guild")
                        .WithMany("UsersArray")
                        .HasForeignKey("GuildId");
                });
        }
    }
}
