using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Diagnostics;

namespace KiteBotCore.Modules.Rank
{
    public class RankService
    {
        private readonly DiscordContextFactory _discordFactory;
        private readonly RankConfigs _rankConfigs;
        private readonly Action<RankConfigs> _saveFunc;

        public RankService(RankConfigs rankConfigs, Action<RankConfigs> saveFunc, DiscordContextFactory discordContextFactory, DiscordSocketClient client)
        {
            _discordFactory = discordContextFactory;
            _rankConfigs = rankConfigs;
            _saveFunc = saveFunc;

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

        public async Task AddUser(SocketGuildUser userInput)
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
                //await UpdateUserRoles(userInput).ConfigureAwait(false);
            }
        }

        public async Task UpdateLastActivity(SocketMessage message)
        {
            SocketGuildChannel messageGuildChannel = (message.Channel as SocketGuildChannel);
            if (messageGuildChannel != null && _rankConfigs.GuildConfigs.ContainsKey(messageGuildChannel.Guild.Id))
            {
                using (KiteBotDbContext db = _discordFactory.Create(new DbContextFactoryOptions()))
                {
                    var user = await db.FindAsync<User>(message.Author.Id.ConvertToUncheckedLong()).ConfigureAwait(false);
                    user.LastActivityAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        public Task UpdateUserRoles(SocketGuildUser user)
        {
            throw new NotImplementedException();
        }

        public void AddRank(ulong guildId, ulong roleId, TimeSpan timeSpan)
        {
            RankConfigs.GuildRanks ranks;
            if (_rankConfigs.GuildConfigs.TryGetValue(guildId, out ranks))
            {
                ranks.Ranks.Add(roleId, new RankConfigs.GuildRanks.Rank
                {
                    RoleId = roleId,
                    Colors = new List<ulong>(),
                    RequiredTimeSpan = timeSpan
                });
            }
            else
            {
                _rankConfigs.GuildConfigs.Add(guildId, new RankConfigs.GuildRanks
                {
                    GuildId = guildId,
                    Ranks = new Dictionary<ulong, RankConfigs.GuildRanks.Rank>
                    {
                        {
                            roleId,
                            new RankConfigs.GuildRanks.Rank
                            {
                                RoleId = roleId,
                                Colors = new List<ulong>(),
                                RequiredTimeSpan = timeSpan
                            }
                        }
                    }
                });
            }
            _saveFunc(_rankConfigs);
        }

        public bool RemoveRank(ulong guildId, ulong roleId)
        {
            bool result = _rankConfigs.GuildConfigs[guildId].Ranks.Remove(roleId);
            if (result)
            {
                _saveFunc(_rankConfigs);
            }
            return result;
        }

        public IEnumerable<RankConfigs.GuildRanks.Rank> GetRanksForGuild(ulong guildId)
        {
            return _rankConfigs.GuildConfigs[guildId].Ranks.Values;
        }

        public void AddColorToRank(ulong guildId, ulong roleRankId, ulong colorId)
        {
            _rankConfigs.GuildConfigs[guildId].Ranks[roleRankId].Colors.Add(colorId);
            _saveFunc(_rankConfigs);
        }

        public bool RemoveColorFromRank(ulong guildId, ulong rankRoleId, IRole colorRole)
        {
            bool result = _rankConfigs.GuildConfigs[guildId].Ranks[rankRoleId].Colors.Remove(colorRole.Id);
            _saveFunc(_rankConfigs);
            return result;
        }

        public void EnableGuildRanks(ulong guildId)
        {
            _rankConfigs.GuildConfigs.Add(guildId, new RankConfigs.GuildRanks
            {
                GuildId = guildId,
                Ranks = new Dictionary<ulong, RankConfigs.GuildRanks.Rank>()
            });
            _saveFunc(_rankConfigs);
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

