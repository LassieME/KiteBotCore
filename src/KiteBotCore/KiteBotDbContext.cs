using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KiteBotCore
{
    public class KiteBotDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<UserColorRoles> UserColorRoles { get; set; }
        public DbSet<Event> Events { get; set; }
        private string ConnectionString { get; }

        public KiteBotDbContext(string settingsDatabaseConnectionString)
        {
            ConnectionString = settingsDatabaseConnectionString;
        }

        public KiteBotDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (ConnectionString != null)//this is wrong, but it works
                optionsBuilder.UseNpgsql(ConnectionString).EnableSensitiveDataLogging();
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Channel>()
                .HasOne(g => g.Guild)
                .WithMany(u => u.Channels)
                .IsRequired();

            modelBuilder.Entity<User>()
                .HasOne(g => g.Guild)
                .WithMany(u => u.Users)
                .IsRequired();

            modelBuilder.Entity<Message>()
                .HasOne(c => c.Channel)
                .WithMany(m => m.Messages)
                .IsRequired();

            modelBuilder.Entity<Message>()
                .HasOne(u => u.User)
                .WithMany(m => m.Messages)
                .IsRequired();

            //Composite Key, User Roles
            modelBuilder.Entity<UserColorRoles>()
                .HasKey(t => new { t.UserId, t.RoleId });

            modelBuilder.Entity<UserColorRoles>()
                .HasOne(pt => pt.User)
                .WithMany(p => p.UserRoles)
                .HasForeignKey(pt => pt.UserId);

            //Events
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Guild)
                .WithMany(g => g.Events)
                .IsRequired();

            modelBuilder.Entity<Event>()
                .HasOne(e => e.User)
                .WithMany(u => u.Events)
                .IsRequired();

            modelBuilder.Entity<ShineBetEvent>()
                .HasMany(e => e.ShineBets)
                .WithOne(b => b.ShineBetEvent);

            modelBuilder.Entity<ShineBet>()
                .HasOne(b => b.ShineBetEvent)
                .WithMany(e => e.ShineBets)
                .IsRequired();

            modelBuilder.Entity<ShineBet>()
                .HasOne(e => e.User)
                .WithMany(u => u.Bets);
                
        }
    }

    public class ShineBetEvent
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ShineBetEventId { get; set; }

        public DateTimeOffset CreationDateTime { get; set; } = DateTimeOffset.UtcNow;        

        public TimeSpan TimeUntilClose { get; set; }

        [Required]
        public string Question { get; set; }

        public int BetAmount { get; set; }

        public bool? BetResult { get; set; }

        [Required]
        public User OwnerUser { get; set; }

        [Required]
        public Channel Channel { get; set; }

        [Required]
        public Guild Guild { get; set; }

        [Required]//Will always have the creator
        public virtual List<ShineBet> ShineBets { get; set; }
    }

    public class ShineBet
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ShineBetId { get; set; }        

        public bool Answer { get; set; }

        public bool DoubleDown { get; set; }

        public User User { get; set; }

        public ShineBetEvent ShineBetEvent { get; set; }
    }

    public class Guild
    {
        [Key]
        public long GuildId { get; set; }

        [NotMapped]
        public ulong Id
        {
            get{ unchecked{ return (ulong)GuildId; } }
            set{ unchecked{ GuildId = (long)value; } }
        }

        public string Name { get; set; }

        public List<Channel> Channels { get; set; }

        public virtual List<User> Users { get; set; }

        public virtual List<Event> Events { get; set; }
    }

    public class Event
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EventId { get; set; }

        public DateTimeOffset DateTime { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public User User { get; set; }

        public Guild Guild { get; set; }
    }

    public class User
    {
        [Key]
        public long UserId { get; set; }

        [NotMapped]
        public ulong Id
        {
            get{ unchecked{ return (ulong)UserId; } }
            set{ unchecked{ UserId = (long)value; } }
        }

        public string Name { get; set; }

        public int Shines { get; set; }

        [NotMapped] public string RegToken { get; set; }

        [NotMapped] public bool Premium { get; set; }

        [NotMapped] public DateTimeOffset? PremiumLastCheckedAt { get; set; }

        public DateTimeOffset LastActivityAt { get; set; }

        public DateTimeOffset? JoinedAt { get; set; }

        //public bool OptOut { get; set; }

        [ForeignKey("GuildForeignKey")]
        public Guild Guild { get; set; }

        public virtual List<UserColorRoles> UserRoles { get; set; }

        public virtual List<Event> Events { get; set; }

        public virtual List<Message> Messages { get; set; }

        public virtual List<ShineBet> Bets { get; set; }
    }

    public class UserColorRoles : IColor
    {
        public long UserId { get; set; }

        [NotMapped]
#pragma warning disable IDE1006 // Naming Styles
        public ulong uId
#pragma warning restore IDE1006 // Naming Styles
        {
            get { unchecked { return (ulong)UserId; } }
            set { unchecked { UserId = (long)value; } }
        }

        public long RoleId { get; set; }

        [NotMapped]
        public ulong Id
        {
            get { unchecked { return (ulong)RoleId; } }
            set { unchecked { RoleId = (long)value; } }
        }

        public DateTimeOffset? RemovalAt { get; set; }

        public User User { get; set; }
    }

    public class Channel
    {
        [Key]
        public long ChannelId { get; set; }

        [NotMapped]
        public ulong Id
        {
            get{ unchecked{ return (ulong)ChannelId; } }
            set{ unchecked{ ChannelId = (long)value; } }
        }

        public string Name { get; set; }

        [ForeignKey("GuildForeignKey")]
        public Guild Guild { get; set; }

        public virtual List<Message> Messages { get; set; }
    }

    public class Message
    {
        [Key]
        public long MessageId { get; set; }

        [NotMapped]
        public ulong Id
        {
            get{ unchecked{ return (ulong)MessageId; } }
            set{ unchecked{ MessageId = (long)value; } }
        }

        public string Content { get; set; }

        [ForeignKey("UserForeignKey")]
        public virtual User User { get; set; }

        [ForeignKey("ChannelForeignKey")]
        public virtual Channel Channel { get; set; }
    }

    public static class UlongArrayHelper
    {
        public static long[] ConvertToUncheckedLongArray(this ulong[] ulongArray)
        {
            long[] longArray = new long[ulongArray.Length];
            unchecked
            {
                for (int i = 0; i < ulongArray.Length; i++)
                    longArray[i] = (long)ulongArray[i];
            }
            return longArray;
        }

        public static ulong[] ConvertToUncheckedULongArray(this long[] longArray)
        {
            ulong[] ulongArray = new ulong[longArray.Length];
            unchecked
            {
                for (int i = 0; i < longArray.Length; i++)
                    ulongArray[i] = (ulong)longArray[i];
            }
            return ulongArray;
        }
    }
}
