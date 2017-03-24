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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace KiteBotCore
{
    public class DiscordContextFactory : IDbContextFactory<DiscordContext>
    {
        private static string SettingsPath => Directory.GetCurrentDirectory().Replace(@"\bin\Debug\netcoreapp1.1\","") + "/Content/settings.json";
        public DiscordContext Create(DbContextFactoryOptions options) //TODO: Make this actually use the options, whoops
        {
            var settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(SettingsPath));
            return new DiscordContext(settings.DatabaseConnectionString);
        }
    }

    internal static class DiscordContextFactoryExtensions
    {
        internal static async Task SyncGuild(this DiscordContextFactory dbFactory, SocketGuild socketGuild)
        {
            var downloadUserTask = socketGuild.DownloadUsersAsync();
            using (var dbContext = dbFactory.Create(new DbContextFactoryOptions()))
            {
                Guild guild = await dbContext.Guilds
                    .Include(g => g.Channels)
                    .Include(g => g.Users)
                    .FirstAsync(x => x.Id == socketGuild.Id)
                    .ConfigureAwait(false);
                    
                if (guild == null)
                    //If guild does not exist, we create a new one and populate it with Users and Channels
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

                    await downloadUserTask.ConfigureAwait(false);
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
                    //I should also probably track when channels no longer exist, but its probably not a big deal right now
                    
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

                    await downloadUserTask.ConfigureAwait(false);
                    var usersNotTracked = socketGuild.Users.Where(x => !guild.Users.Any(y => y.Id == x.Id));
                        //Any stops at the first occurence, All checks all elements
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

    public class DiscordContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Message> Messages { get; set; }
        private string ConnectionString { get; }

        public DiscordContext(string settingsDatabaseConnectionString)
        {
            ConnectionString = settingsDatabaseConnectionString;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(ConnectionString);
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

        public virtual List<Channel> Channels { get; set; }
        
        public virtual List<User> Users { get; set; }}

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

        public DateTimeOffset LastActivityAt { get; set; }

        public DateTimeOffset? JoinedAt { get; set; }

        [ForeignKey("GuildForeignKey")]
        public virtual Guild Guild { get; set; }

        public virtual List<Message> Messages { get; set; }
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
        public virtual Guild Guild { get; set; }

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
}
