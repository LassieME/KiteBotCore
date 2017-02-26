using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Diagnostics;

namespace KiteBotCore.Modules.Rank
{
    class RankService
    {
        private DiscordContextFactory _discordFactory;

        public RankService(DiscordContextFactory discordContextFactory, DiscordSocketClient client)
        {
            _discordFactory = discordContextFactory;
            client.UserJoined += AddUser;

        }

        public async Task<DateTimeOffset> CheckUserJoinDate(IUser inputUser, IGuild guild)
        {
            using (DiscordContext db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                unchecked
                {
                    long key = (long) inputUser.Id;
                    User user = await db.FindAsync<User>(key);

                    Debug.Assert(user != null);
                    return user.JoinedAt.Value;                    
                }                
            }
        }

        internal async Task AddUser(SocketGuildUser userInput)
        {
            using (DiscordContext db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                unchecked
                {
                    long guildKey = (long)userInput.Guild.Id;
                    Guild guild = await db.FindAsync<Guild>(guildKey);

                    long userKey = (long)userInput.Id;
                    User user = await db.FindAsync<User>(userKey);

                    if (user == null)
                    {
                        user = new User()
                        {
                            Name = userInput.Username,
                            Guild = guild,
                            Id = userInput.Id,
                            JoinedAt = userInput.JoinedAt,
                            LastActivityAt = DateTimeOffset.UtcNow,
                            Messages = new List<Message>()
                        };
                        guild.Users.Add(user);
                    }
                    else
                    {
                        user.LastActivityAt = DateTimeOffset.UtcNow;
                    }
                    await db.SaveChangesAsync();
                    await UpdateUserRoles(userInput);
                }
            }
        }

        internal async Task UpdateUserRoles(SocketGuildUser user)
        {

        }
    }
}
