using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using KiteBotCore;

namespace KiteBotCore.Migrations
{
    [DbContext(typeof(KiteBotDbContext))]
    [Migration("20170810212408_OptOutMigration")]
    partial class OptOutMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "1.1.2");

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

            modelBuilder.Entity("KiteBotCore.Event", b =>
                {
                    b.Property<int>("EventId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

                    b.Property<DateTimeOffset>("DateTime");

                    b.Property<string>("Description");

                    b.Property<long?>("GuildId")
                        .IsRequired();

                    b.Property<string>("Title");

                    b.Property<long?>("UserId")
                        .IsRequired();

                    b.HasKey("EventId");

                    b.HasIndex("GuildId");

                    b.HasIndex("UserId");

                    b.ToTable("Events");
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

                    b.Property<bool>("OptOut");

                    b.HasKey("UserId");

                    b.HasIndex("GuildForeignKey");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("KiteBotCore.UserColorRoles", b =>
                {
                    b.Property<long>("UserId");

                    b.Property<long>("Id");

                    b.Property<DateTimeOffset?>("RemovalAt");

                    b.HasKey("UserId", "Id");

                    b.ToTable("UserColorRoles");
                });

            modelBuilder.Entity("KiteBotCore.Channel", b =>
                {
                    b.HasOne("KiteBotCore.Guild", "Guild")
                        .WithMany("Channels")
                        .HasForeignKey("GuildForeignKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("KiteBotCore.Event", b =>
                {
                    b.HasOne("KiteBotCore.Guild", "Guild")
                        .WithMany("Events")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("KiteBotCore.User", "User")
                        .WithMany("Events")
                        .HasForeignKey("UserId")
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

            modelBuilder.Entity("KiteBotCore.UserColorRoles", b =>
                {
                    b.HasOne("KiteBotCore.User", "User")
                        .WithMany("UserRoles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
