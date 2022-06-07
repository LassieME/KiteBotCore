using System;
using System.Collections.Generic;
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
        private static string SettingsPath => Directory.GetCurrentDirectory().Replace(@"\bin\Debug\netcoreapp1.1.2\", "") + "/Content/settings.json";

        public KiteBotDbContext Create() => CreateDbContext(new string[] { });

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
            using (var dbContext = dbFactory.Create())
            {
                Guild guild = (await dbContext.Guilds
                    .Include(g => g.Channels)
                    .Include(g => g.Users)
                    .ToListAsync()).FirstOrDefault(x => x.Id == socketGuild.Id);

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
                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    //This should also probably track when channels no longer exist, but its probably not a big deal right now

                    var channelsNotTracked = socketGuild.TextChannels.Where(x => guild.Channels.All(y => y.Id != x.Id));
                    var socketTextChannels = channelsNotTracked as IList<SocketTextChannel> ??
                                             channelsNotTracked.ToList();
                    if (socketTextChannels.Count > 0)
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
                        await dbContext.SaveChangesAsync();
                    }
                    
                    var usersNotTracked = socketGuild.Users.Where(x => guild.Users.All(y => y.Id != x.Id));
                    var socketGuildUsers = usersNotTracked as IList<SocketGuildUser> ?? usersNotTracked.ToList();
                    if (socketGuildUsers.Count > 0)
                    {
                        foreach (var userToTrack in socketGuildUsers)
                        {
                            User user = new()
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
                        await dbContext.SaveChangesAsync();
                    }                    
                }                
            }
        }
    }
}
