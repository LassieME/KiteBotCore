using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using KiteBotCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace KiteBotCore
{
    public class DiscordContextFactory : IDesignTimeDbContextFactory<KiteBotDbContext> // IDbContextFactory<KiteBotDbContext>
    {
        //This is needed while doing Database migrations and updates
        private static string SettingsPath => Directory.GetCurrentDirectory().Replace(@"\bin\Debug\netcoreapp1.1.2\","") + "/Content/settings.json";

        public KiteBotDbContext Create(DbContextFactoryOptions options) => Create();

        public KiteBotDbContext Create() => CreateDbContext(new string[]{});

        public KiteBotDbContext CreateDbContext(string[] args)
        {
            var settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(SettingsPath));
            return new KiteBotDbContext(settings.DatabaseConnectionString);
        }
    }

    internal static class DiscordContextFactoryExtensions
    {
        internal static async Task SyncGuild(this DiscordContextFactory dbFactory, SocketGuild socketGuild)
        {
            using (var dbContext = dbFactory.Create(new DbContextFactoryOptions()))
            {
                Guild guild = await dbContext.Guilds
                    .Include(g => g.Channels)
                    .Include(g => g.Users)
                    .SingleOrDefaultAsync(x => x.Id == socketGuild.Id).ConfigureAwait(false);

                //If guild does not exist, we create a new one and populate it with Users and Channels
                if (guild == null)
                {
                    guild = new Guild
                    {
                        Id = socketGuild.Id,
                        Channels = new List<Channel>(),
                        Name = socketGuild.Name,
                        Users = new List<User>()
                    };

                    foreach (var textChannel in socketGuild.TextChannels)
                    {
                        Channel channel = new Channel
                        {
                            Id = textChannel.Id,
                            Guild = guild,
                            Name = textChannel.Name,
                            Messages = new List<Message>()
                        };
                        guild.Channels.Add(channel);
                    }

                    foreach (var socketUser in socketGuild.Users)
                        //For now, Users are unique for each guild, this will cause me problems later I'm sure
                    {
                        User user = new User
                        {
                            Id = socketUser.Id,
                            Guild = guild,
                            LastActivityAt = DateTimeOffset.UtcNow,
                            JoinedAt = socketUser.JoinedAt,
                            Messages = new List<Message>(),
                            Name = socketUser.Username
                        };
                        guild.Users.Add(user);
                    }
                    dbContext.Add(guild);
                }
                else
                {
                    //This should also probably track when channels no longer exist, but its probably not a big deal right now
                    
                    var channelsNotTracked = socketGuild.TextChannels.Where(x => guild.Channels.All(y => y.Id != x.Id));
                    var socketTextChannels = channelsNotTracked as IList<SocketTextChannel> ??
                                             channelsNotTracked.ToList();
                    if (socketTextChannels.Any())
                    {
                        foreach (var channelToTrack in socketTextChannels)
                        {
                            Channel channel = new Channel
                            {
                                Guild = guild,
                                Id = channelToTrack.Id,
                                Messages = new List<Message>(),
                                Name = channelToTrack.Name
                            };
                            guild.Channels.Add(channel);
                        }
                        dbContext.Update(guild);
                    }

                    var usersNotTracked = socketGuild.Users.Where(x => guild.Users.All(y => y.Id != x.Id));
                    var socketGuildUsers = usersNotTracked as IList<SocketGuildUser> ?? usersNotTracked.ToList();
                    if (socketGuildUsers.Any())
                    {
                        foreach (var userToTrack in socketGuildUsers)
                        {
                            User user = new User
                            {
                                Id = userToTrack.Id,
                                Guild = guild,
                                LastActivityAt = DateTimeOffset.UtcNow,
                                JoinedAt = userToTrack.JoinedAt,
                                Messages = new List<Message>(),
                                Name = userToTrack.Username
                            };
                            guild.Users.Add(user);
                        }
                        dbContext.Update(guild);
                    }
                }
                await dbContext.SaveChangesAsync().ConfigureAwait(false); //I could move this inside the branches, but its relatively cheap to call this if nothing has changed, and avoids multiple calls to it
            }
        }
    }

    public class KiteBotDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<UserColorRoles> UserColorRoles { get; set; }
        public DbSet<Event> Events { get; set; }
        private string ConnectionString { get; set; }

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

        }
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

        //public string RegToken { get; set; }

        //public bool Premium { get; set; }

        public DateTimeOffset LastActivityAt { get; set; }

        public DateTimeOffset? JoinedAt { get; set; }

        public bool OptOut { get; set; }

        [ForeignKey("GuildForeignKey")]
        public Guild Guild { get; set; }

        public virtual List<UserColorRoles> UserRoles { get; set; }

        public virtual List<Event> Events { get; set; }

        public virtual List<Message> Messages { get; set; }
    }

    public class UserColorRoles : IColor
    {
        public long UserId { get; set; }

        [NotMapped]
        public ulong uId
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
