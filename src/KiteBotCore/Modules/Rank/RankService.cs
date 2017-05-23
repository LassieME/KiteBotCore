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
    internal class RankService
    {
        private readonly DiscordContextFactory _discordFactory;

        public RankService(DiscordContextFactory discordContextFactory, DiscordSocketClient client)
        {
            _discordFactory = discordContextFactory;
            client.UserJoined += AddUser;
            client.MessageReceived += UpdateLastActivity;

        }

        public async Task<DateTimeOffset> CheckUserJoinDate(IUser inputUser, IGuild guild)
        {
            using (KiteBotDbContext db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                User user = await db.FindAsync<User>(inputUser.Id.ConvertToUncheckedLong()).ConfigureAwait(false);

                Debug.Assert(user != null);
                Debug.Assert(user.JoinedAt != null, "user.JoinedAt != null");
                return user.JoinedAt.Value;

            }
        }

        internal async Task AddUser(SocketGuildUser userInput)
        {
            using (KiteBotDbContext db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                Guild guild = await db.FindAsync<Guild>(userInput.Guild.Id.ConvertToUncheckedLong()).ConfigureAwait(false);
                User user = await db.FindAsync<User>(userInput.Id.ConvertToUncheckedLong()).ConfigureAwait(false);

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
                await db.SaveChangesAsync().ConfigureAwait(false);
                await UpdateUserRoles(userInput).ConfigureAwait(false);
            }
        }

        internal Task UpdateUserRoles(SocketGuildUser user)
        {
            throw new NotImplementedException();
        }

        internal async Task UpdateLastActivity(SocketMessage message)
        {
            SocketGuildChannel messageGuildChannel = (message.Channel as SocketGuildChannel);
            if (messageGuildChannel != null)
            {
                using (KiteBotDbContext db = _discordFactory.Create(new DbContextFactoryOptions()))
                {
                    var user = await db.FindAsync<User>(message.Author.Id.ConvertToUncheckedLong()).ConfigureAwait(false);
                    user.LastActivityAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }
    }

    public static class IdHelper
    {
        public static long ConvertToUncheckedLong(this ulong u)
        {
            unchecked { return (long)u; }
        }
    }
}

