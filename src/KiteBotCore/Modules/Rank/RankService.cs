using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Serilog;

namespace KiteBotCore.Modules.Rank
{
    public class RankService
    {
        private readonly DiscordContextFactory _discordFactory;
        private readonly RankConfigs _rankConfigs;
        private readonly Action<RankConfigs> _saveFunc;
        private readonly ConcurrentQueue<(IGuildUser user, DateTimeOffset lastActivityAt)> _activityQueue;
        private readonly ConcurrentQueue<(IGuildUser user, IEnumerable<ulong> rolesToAdd , IEnumerable<ulong> rolesToRemove)> _roleUpdateQueue;
        private readonly Timer _roleTimer;
        private readonly Timer _activityTimer;

        public RankService(RankConfigs rankConfigs, Action<RankConfigs> saveFunc, DiscordContextFactory discordContextFactory, DiscordSocketClient client)
        {
            _discordFactory = discordContextFactory;
            _rankConfigs = rankConfigs;
            _saveFunc = saveFunc;
            _activityQueue = new ConcurrentQueue<(IGuildUser, DateTimeOffset)>();
            _roleUpdateQueue = new ConcurrentQueue<(IGuildUser user, IEnumerable<ulong> rolesToAdd, IEnumerable<ulong> rolesToRemove)>();

            client.UserJoined += AddUser;
            client.MessageReceived += UpdateLastActivity;
            _roleTimer = new Timer(async e => await Task.Run(async () => await RoleTimerTask().ConfigureAwait(false)).ConfigureAwait(false), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            _activityTimer = new Timer(async e => await Task.Run(async () => await ActivityTimerTask().ConfigureAwait(false)).ConfigureAwait(false), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private async Task ActivityTimerTask()
        {
            using (KiteBotDbContext db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                while (!_activityQueue.IsEmpty)
                    if (_activityQueue.TryDequeue(out var item))
                    {
                        User user = await db.FindAsync<User>(item.user.Id.ConvertToUncheckedLong()).ConfigureAwait(false);
                        user.LastActivityAt = DateTimeOffset.UtcNow;
                    }
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private async Task RoleTimerTask()
        {
            _roleTimer.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                using (KiteBotDbContext db = _discordFactory.Create(new DbContextFactoryOptions()))
                {
                    while (!_roleUpdateQueue.IsEmpty)
                        if (_roleUpdateQueue.TryDequeue(out var item))
                        {
                            List<ulong> userRoles = item.user.RoleIds.ToList();
                            IEnumerable<ulong> newRoles = userRoles.Where(x => !item.rolesToRemove.Contains(x)).Union(item.rolesToAdd);
                            await item.user.ModifyAsync(x => x.RoleIds = new Optional<IEnumerable<ulong>>(newRoles)).ConfigureAwait(false);
                        }
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Something Happened in RoleTimerTask");
            }
            finally
            {
                _roleTimer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
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
                    user = new User
                    {
                        Name = userInput.Username,
                        Guild = guild,
                        Id = userInput.Id,
                        JoinedAt = userInput.JoinedAt,
                        LastActivityAt = DateTimeOffset.UtcNow,
                        Messages = new List<Message>()
                    };
                    guild.Users.Add(user);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
                else
                {
                    _activityQueue.Enqueue((userInput, DateTimeOffset.UtcNow));
                }
            }
        }

        internal Task UpdateLastActivity(SocketMessage message)
        {
            var messageGuildChannel = message.Channel as SocketGuildChannel;
            var messageAuthor = message.Author as SocketGuildUser;
            if (messageGuildChannel != null && messageAuthor != null && _rankConfigs.GuildConfigs.ContainsKey(messageGuildChannel.Guild.Id))
                _activityQueue.Enqueue((messageAuthor, DateTimeOffset.UtcNow));
            return Task.CompletedTask;
        }

        public async Task<(SocketGuildUser user, ulong[] rolesToAdd, ulong[] rolesToRemove)> AddAndRemoveMissingRanks(SocketGuildUser messageAuthor, IGuild guild)
        {
            var result = (await GetAwardedRoles(messageAuthor, guild).ConfigureAwait(false)).ToList();
            var missingRoles = result
                .Where(x => messageAuthor.Roles.Select(y => y.Id).All(y => y != x.RoleId))//.Any(y => y.Id == x.RoleId))
                .Select(x => x.RoleId).ToArray();
            var rolesToRemove = messageAuthor.Roles
                .Where(x => GetRanksForGuild(guild.Id)
                    .Select(y => y.RoleId).Contains(x.Id) && !result.Select(y => y.RoleId).Contains(x.Id))
                    .Select(x => x.Id).ToArray();
            if (missingRoles.Any() || rolesToRemove.Any())
            {
                //_roleUpdateQueue.Enqueue((messageAuthor, missingRoles, rolesToRemove)); //TODO: remove roles if inactive
                return (messageAuthor, missingRoles, rolesToRemove);
            }
            else
            {
                throw new NotSupportedException("");
            }
        }

        public async Task<DateTimeOffset> GetUserJoinDate(IUser inputUser, IGuild guild)
        {
            using (KiteBotDbContext db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                User user = await db.FindAsync<User>(inputUser.Id.ConvertToUncheckedLong()).ConfigureAwait(false);

                Debug.Assert(user != null);
                Debug.Assert(user.JoinedAt != null, "user.JoinedAt != null");
                return user.JoinedAt.Value;
            }
        }

        public async Task<IEnumerable<RankConfigs.GuildRanks.Rank>> GetAwardedRoles(IGuildUser user, IGuild guild) //TODO: Make this remove roles if inactive
        {
            DateTimeOffset result = await GetUserJoinDate(user, guild).ConfigureAwait(false);
            TimeSpan timeInGuild = DateTimeOffset.UtcNow - result;
            List<RankConfigs.GuildRanks.Rank> guildRanks = GetRanksForGuild(guild.Id).ToList();
            
            return guildRanks.Where(x => x.RequiredTimeSpan < timeInGuild);
        }

        public async Task<IEnumerable<ulong>> GetAvailableColors(IGuildUser user, IGuild guild)
        {
            return (await GetAwardedRoles(user, guild).ConfigureAwait(false)).SelectMany(x => x.Colors);
        }

        /// <summary>
        /// May return null if there is no next rank available
        /// </summary>
        /// <param name="user"></param>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task<(IRole role, TimeSpan timeToRole)> GetNextRole(IGuildUser user, IGuild guild)
        {
            var timeInGuild = DateTimeOffset.UtcNow - await GetUserJoinDate(user, guild).ConfigureAwait(false);
            var nextRank = _rankConfigs.GuildConfigs[guild.Id].Ranks.Values.FirstOrDefault(y => y.RequiredTimeSpan > timeInGuild);
            var guildRole = nextRank != null ? guild.Roles.FirstOrDefault(x => x.Id == nextRank.RoleId) : null;
            return (guildRole, nextRank != null ? nextRank.RequiredTimeSpan - timeInGuild : TimeSpan.MaxValue );
        }

        public void AddRank(ulong guildId, ulong roleId, TimeSpan timeSpan)
        {
            RankConfigs.GuildRanks ranks;
            if (_rankConfigs.GuildConfigs.TryGetValue(guildId, out ranks))
                ranks.Ranks.Add(roleId, new RankConfigs.GuildRanks.Rank
                {
                    RoleId = roleId,
                    Colors = new List<ulong>(),
                    RequiredTimeSpan = timeSpan
                });
            else
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
            _saveFunc(_rankConfigs);
        }

        public bool RemoveRank(ulong guildId, ulong roleId)
        {
            bool result = _rankConfigs.GuildConfigs[guildId].Ranks.Remove(roleId);
            if (result)
                _saveFunc(_rankConfigs);
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

