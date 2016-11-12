using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using KiteBotCore;

namespace KiteBotCore.Migrations
{
    [DbContext(typeof(MarkovChainMContext))]
    [Migration("20161111212408_MyFirstMigration")]
    partial class MyFirstMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.1");

            modelBuilder.Entity("KiteBotCore.Json.MarkovMessage", b =>
                {
                    b.Property<long>("MarkovMessageId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("M");

                    b.HasKey("MarkovMessageId");

                    b.ToTable("Messages");
                });
        }
    }
}
