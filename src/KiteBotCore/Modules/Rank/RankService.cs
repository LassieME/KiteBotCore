using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KiteBotCore.Modules.Rank
{
    public class RankService
    {
        private readonly DiscordContextFactory _discordFactory;
        private readonly DiscordSocketClient _client;
        private readonly RankConfigs _rankConfigs;
        private readonly Action<RankConfigs> _saveFunc;
        private readonly ConcurrentQueue<(IGuildUser user, DateTimeOffset lastActivityAt)> _activityQueue;
        private readonly ConcurrentQueue<(IGuildUser user, IEnumerable<ulong> rolesToAdd, IEnumerable<ulong> rolesToRemove)> _roleUpdateQueue;
        private readonly Timer _roleTimer;
        private readonly Timer _activityTimer;

        public RankService(RankConfigs rankConfigs, Action<RankConfigs> saveFunc, DiscordContextFactory discordContextFactory, DiscordSocketClient client)
        {
            _discordFactory = discordContextFactory;
            _client = client;
            _rankConfigs = rankConfigs;
            _saveFunc = saveFunc;
            _activityQueue = new ConcurrentQueue<(IGuildUser, DateTimeOffset)>();
            _roleUpdateQueue = new ConcurrentQueue<(IGuildUser user, IEnumerable<ulong> rolesToAdd, IEnumerable<ulong> rolesToRemove)>();

            client.UserJoined += AddUser;
            client.MessageReceived += UpdateLastActivity;
            _roleTimer = new Timer(async e => await Task.Run(async () => await RoleTimerTask().ConfigureAwait(false)).ConfigureAwait(false), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));

            _activityTimer = new Timer(async e => await Task.Run(async () => await ActivityTimerTask().ConfigureAwait(false)).ConfigureAwait(false), null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        }

        private async Task ActivityTimerTask()
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                while (!_activityQueue.IsEmpty)
                    if (_activityQueue.TryDequeue(out var item))
                    {
                        try
                        {
                            var user = await db.FindAsync<User>(item.user.Id.ConvertToUncheckedLong())
                                .ConfigureAwait(false);
                            if (user == null)
                            {
                                Log.Information("Found a non-tracked user, adding...");
                                user = await AddUser(item.user).ConfigureAwait(false);
                            }
                            user.LastActivityAt = DateTimeOffset.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Something Happened in ActivityTimerTask");
                        }
                    }
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private async Task RoleTimerTask()
        {
            _roleTimer.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                foreach (var guild in _client.Guilds.Where(x => _rankConfigs.GuildConfigs.Keys.Contains(x.Id)))
                {
                    using (KiteBotDbContext db = _discordFactory.Create(new DbContextFactoryOptions()))
                    {
                        var users = db.Users.ToList();
                        foreach (var socketGuildUser in guild.Users)
                        {
                            try
                            {
                                var joinDate = users.FirstOrDefault(x => x.Id == socketGuildUser.Id).JoinedAt;
                                var activityDate = users.FirstOrDefault(x => x.Id == socketGuildUser.Id).LastActivityAt;
                                var ranks = await GetAwardedRoles(socketGuildUser, guild, joinDate.GetValueOrDefault(),
                                    activityDate).ConfigureAwait(false);
                                await AddAndRemoveMissingRanks(socketGuildUser, guild, ranks).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error in foreach in RoleTimerTask");
                            }
                        }
                    }
                }

                while (!_roleUpdateQueue.IsEmpty)
                {
                    if (_roleUpdateQueue.TryDequeue(out var item))
                    {
                        var userRoles = item.user.RoleIds.ToList();
                        var newRoles = userRoles.Where(x => !item.rolesToRemove.Contains(x)).Union(item.rolesToAdd).ToArray();
                        await item.user.ModifyAsync(x => x.RoleIds = newRoles)
                            .ConfigureAwait(false);
                    }
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

        internal async Task<User> AddUser(IGuildUser userInput)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var guild = await db.Guilds.Include(g => g.Users).SingleOrDefaultAsync(x => x.GuildId == userInput.Guild.Id.ConvertToUncheckedLong()).ConfigureAwait(false);
                var user = await db.FindAsync<User>(userInput.Id.ConvertToUncheckedLong()).ConfigureAwait(false);

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
                return user;
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

        public async Task AddAndRemoveMissingRanks(IGuildUser messageAuthor, IGuild guild, IList<RankConfigs.GuildRanks.Rank> result)
        {
            if(result == null)
                result = (await GetAwardedRoles(messageAuthor, guild).ConfigureAwait(false)).ToList();
            var missingRoles = result
                .Where(x => messageAuthor.RoleIds.All(y => y != x.RoleId))
                .Select(x => x.RoleId)
                .ToArray();
            var rolesToRemove = messageAuthor.RoleIds
                .Where(x => GetRanksForGuild(guild.Id).Select(y => y.RoleId).Contains(x) && !result.Select(y => y.RoleId).Contains(x))
                .ToArray();
            if (missingRoles.Any() || rolesToRemove.Any())
            {
                _roleUpdateQueue.Enqueue((messageAuthor, missingRoles, rolesToRemove));
            }
        }

        public async Task<IList<RankConfigs.GuildRanks.Rank>> GetAwardedRoles(IGuildUser user, IGuild guild, DateTimeOffset joinDate = default(DateTimeOffset), DateTimeOffset lastActivity = default(DateTimeOffset)) //TODO: Make this remove roles if inactive
        {
            if(joinDate == default(DateTimeOffset))
                joinDate = await GetUserJoinDate(user, guild).ConfigureAwait(false);
            if(lastActivity == default(DateTimeOffset))
                lastActivity = await GetUserLastActivity(user, guild).ConfigureAwait(false);
            var timeInGuild = DateTimeOffset.UtcNow - joinDate;
            var guildRanks = GetRanksForGuild(guild.Id);
            var awardedRoles = guildRanks.Where(x => x.RequiredTimeSpan < timeInGuild).ToList();
            var finalRoles = awardedRoles.Take(awardedRoles.Count - (DateTimeOffset.UtcNow - lastActivity).Days / 7);

            return finalRoles.ToList();
        }



        public async Task<DateTimeOffset> GetUserLastActivity(IGuildUser inputUser, IGuild guild)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var user = await db.FindAsync<User>(inputUser.Id.ConvertToUncheckedLong()).ConfigureAwait(false);

                Debug.Assert(user != null);
                Debug.Assert(user.LastActivityAt != null, "user.LastActivityAt != null");
                return user.LastActivityAt;
            }
        }

        public async Task<DateTimeOffset> GetUserJoinDate(IUser inputUser, IGuild guild)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var user = await db.FindAsync<User>(inputUser.Id.ConvertToUncheckedLong()).ConfigureAwait(false);

                Debug.Assert(user != null);
                Debug.Assert(user.JoinedAt != null, "user.JoinedAt != null");
                return user.JoinedAt.Value;
            }
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
            return (guildRole, nextRank != null ? nextRank.RequiredTimeSpan - timeInGuild : TimeSpan.MaxValue);
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
            var result = _rankConfigs.GuildConfigs[guildId].Ranks.Remove(roleId);
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
            var result = _rankConfigs.GuildConfigs[guildId].Ranks[rankRoleId].Colors.Remove(colorRole.Id);
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

        public async Task SetJoinDate(IUser user, ulong guildId, DateTimeOffset newDate)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var dbUser = await db.Users.FindAsync(user.Id.ConvertToUncheckedLong()).ConfigureAwait(false);
                dbUser.JoinedAt = newDate;
                await db.SaveChangesAsync().ConfigureAwait(false);
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

