using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Serilog;
using Color = KiteBotCore.Json.Color;

namespace KiteBotCore.Modules.RankModule
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

            _roleTimer = new Timer(async e => await Task.Run(async () => await RoleTimerTask().ConfigureAwait(false)).ConfigureAwait(false), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));

            _activityTimer = new Timer(async e => await Task.Run(async () => await ActivityTimerTask().ConfigureAwait(false)).ConfigureAwait(false), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        //TODO: Should probably not be here
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


        private async Task ActivityTimerTask()
        {
            if (!_activityQueue.IsEmpty)
            {
                using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
                {
                    await db.Users.LoadAsync().ConfigureAwait(false);
                    while (!_activityQueue.IsEmpty)
                        if (_activityQueue.TryDequeue(out var item))
                        {
                            try
                            {
                                var user = await db.FindAsync<User>(item.user.Id.ConvertToUncheckedLong())
                                    .ConfigureAwait(false);
                                if(user.OptOut == false)
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
                        var users = db.Users.Include(u => u.UserRoles).ToList();
                        foreach (var socketGuildUser in guild.Users.Where(x => !x.IsBot))
                        {
                            try
                            {
                                var dbUser = users.FirstOrDefault(x => x.Id == socketGuildUser.Id);
                                if (dbUser.OptOut == false)
                                {
                                    var joinDate = dbUser.JoinedAt;
                                    var activityDate = dbUser.LastActivityAt;

                                    var ranks = await GetAwardedRoles(socketGuildUser, guild,
                                        joinDate.GetValueOrDefault(),
                                        activityDate).ConfigureAwait(false);

                                    var assignedColorsToRemove = dbUser?.UserRoles?.OrEmptyIfNull()
                                        .Where(x => x.RemovalAt != null && x.RemovalAt < DateTimeOffset.UtcNow)
                                        .ToList();

                                    Debug.Assert(assignedColorsToRemove != null, "assignedColorsToRemove != null");
                                    foreach (var colorRoles in assignedColorsToRemove)
                                    {
                                        dbUser.UserRoles.Remove(colorRoles);
                                    }
                                    await AddAndRemoveMissingRanks(socketGuildUser, guild, ranks,
                                        assignedColorsToRemove).ConfigureAwait(false);
                                }
                                else
                                {
                                    var ranks = _rankConfigs.GuildConfigs[guild.Id].Ranks.Values
                                        .Where(x => socketGuildUser.Roles.Any(y => y.Id == x.RoleId))
                                        .ToList();

                                    var assignedColorsToRemove = dbUser?.UserRoles?.OrEmptyIfNull()
                                        .ToList();

                                    await AddAndRemoveMissingRanks(socketGuildUser, guild, ranks, assignedColorsToRemove).ConfigureAwait(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error in foreach in RoleTimerTask");
                            }
                        }
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }

                while (!_roleUpdateQueue.IsEmpty)
                {
                    if (_roleUpdateQueue.TryDequeue(out var item))
                    {
                        var userRoles = item.user.RoleIds.ToList();
                        var newRoles = userRoles.Where(x => !item.rolesToRemove.Contains(x)).Union(item.rolesToAdd).ToArray();
                        await item.user.ModifyAsync(x => x.RoleIds = newRoles) //Race-condition, might change it later when there is less pressure on the rate-limit
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
                _roleTimer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));
            }
        }

        internal async Task AddAndRemoveMissingRanks(IGuildUser messageAuthor, IGuild guild, IList<Rank> result, IList<UserColorRoles> userColorToRemove)
        {
            if(result == null)
                result = (await GetAwardedRoles(messageAuthor, guild).ConfigureAwait(false)).ToList();

            var missingRoles = result
                .Where(x => messageAuthor.RoleIds.All(y => y != x.RoleId))
                .Select(x => x.RoleId)
                .ToArray();
            var rolesToRemove = messageAuthor.RoleIds.Where(x => 
                GetRanksForGuild(guild.Id).Select(y => y.RoleId).Contains(x) && !result.Select(y => y.RoleId).Contains(x));

            var colors = result.SelectMany(x => x.Colors).Select(y => y.Id);
            var allColorIds = GetRanksForGuild(guild.Id).SelectMany(x => x.Colors).Select(y => y.Id);
            var currentColors = messageAuthor.RoleIds.Intersect(allColorIds).ToArray();

            if (!colors.Any() && currentColors.Any())
            {
                rolesToRemove = rolesToRemove.Union(currentColors);
            }

            if (userColorToRemove.Any(x => messageAuthor.RoleIds.Contains(x.Id)))
            {
                rolesToRemove = rolesToRemove.Union(userColorToRemove.Select(x => x.Id));
            }

            var toRemove = rolesToRemove as ulong[] ?? rolesToRemove.ToArray();
            if (missingRoles.Any() || toRemove.Any())
            {
                _roleUpdateQueue.Enqueue((messageAuthor, missingRoles, toRemove));
            }
        }

        public async Task<IList<Rank>> GetAwardedRoles(IGuildUser user, IGuild guild, DateTimeOffset joinDate = default(DateTimeOffset), DateTimeOffset lastActivity = default(DateTimeOffset))
        {
            if(joinDate == default(DateTimeOffset))
                joinDate = await GetUserJoinDate(user, guild).ConfigureAwait(false);
            if(lastActivity == default(DateTimeOffset))
                lastActivity = await GetUserLastActivity(user, guild).ConfigureAwait(false);
            var timeInGuild = DateTimeOffset.UtcNow - joinDate;

            return GetRanksForGuild(guild.Id)
                .Where(x => x.RequiredTimeSpan < timeInGuild)
                .Take(GetRanksForGuild(guild.Id)
                    .Where(x => x.RequiredTimeSpan < timeInGuild).ToList()
                    .Count - (DateTimeOffset.UtcNow - lastActivity).Days / 7).ToList();
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

        public async Task<IEnumerable<IColor>> GetAvailableColors(IGuildUser user, IGuild guild)
        {
            //Gets regular colors awarded by reaching ranks
            var awardedRoles = (await GetAwardedRoles(user, guild).ConfigureAwait(false)).SelectMany(x => x.Colors);
            //Gets colors assigned my admins/mods
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var dbUser = await db.Users
                    .Include(u => u.UserRoles)
                    .SingleOrDefaultAsync(x => x.Id == user.Id).ConfigureAwait(false);
                if (dbUser.UserRoles != null && dbUser.UserRoles.Any())
                {
                    IEnumerable<IColor> assignedRoles = dbUser.UserRoles;
                    return awardedRoles.Union(assignedRoles);
                }
                return awardedRoles;
            }
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
            GuildRanks ranks;
            if (_rankConfigs.GuildConfigs.TryGetValue(guildId, out ranks))
                ranks.Ranks.Add(roleId, new Rank
                {
                    RoleId = roleId,
                    Colors = new List<Color>(),
                    RequiredTimeSpan = timeSpan
                });
            else
                _rankConfigs.GuildConfigs.Add(guildId, new GuildRanks
                {
                    GuildId = guildId,
                    Ranks = new Dictionary<ulong, Rank>
                    {
                        {
                            roleId,
                            new Rank
                            {
                                RoleId = roleId,
                                Colors = new List<Color>(),
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

        public IEnumerable<Rank> GetRanksForGuild(ulong guildId)
        {
            return _rankConfigs.GuildConfigs[guildId].Ranks.Values;
        }

        public void AddColorToRank(ulong guildId, ulong roleRankId, ulong colorId)
        {
            var color = new Color {Id = colorId};
            _rankConfigs.GuildConfigs[guildId].Ranks[roleRankId].Colors.Add(color);
            _saveFunc(_rankConfigs);
        }

        public bool RemoveColorFromRank(ulong guildId, ulong rankRoleId, IRole colorRole)
        {
            var colorToRemove = _rankConfigs.GuildConfigs[guildId].Ranks[rankRoleId].Colors
                .FirstOrDefault(x => x.Id == colorRole.Id);
            var result = _rankConfigs.GuildConfigs[guildId].Ranks[rankRoleId].Colors.Remove(colorToRemove);
            _saveFunc(_rankConfigs);
            return result;
        }

        public void EnableGuildRanks(ulong guildId)
        {
            _rankConfigs.GuildConfigs.Add(guildId, new GuildRanks
            {
                GuildId = guildId,
                Ranks = new Dictionary<ulong, Rank>()
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

        public async Task<bool> AssignColorToUser(ulong guildId, ulong userId, ulong colorRoleId, TimeSpan? timeToRemove)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var user = await db.Users
                    .Include(u => u.UserRoles)
                    .SingleOrDefaultAsync(x => x.Id == userId).ConfigureAwait(false);
                if (user.UserRoles == null)
                {
                    user.UserRoles = new List<UserColorRoles>();
                }

                if (user.UserRoles.Any(x => x.Id == colorRoleId))
                {
                    return false;
                }
                user.UserRoles.Add(new UserColorRoles()
                {
                    Id = colorRoleId,
                    RemovalAt = timeToRemove == null ? null : DateTimeOffset.UtcNow + timeToRemove,
                    uId = userId,
                    User = user
                });
                await db.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
        }

        public async Task<bool> UnassignColorFromUserAsync(IUser user, IRole role)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var dbUser = await db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(x => x.UserId == user.Id.ConvertToUncheckedLong()).ConfigureAwait(false);
                var roleToRemove = dbUser.UserRoles.FirstOrDefault(x => x.RoleId == user.Id.ConvertToUncheckedLong());
                if (roleToRemove != null)
                {
                    dbUser.UserRoles.Remove(roleToRemove);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
                else
                    return false;
                return true;
            }
        }

        public async Task<IList<(ulong roleId, DateTimeOffset? expiry)>> GetAssignedRolesAsync(ulong guildId, ulong userId)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var listOfRoles = new List<(ulong roleId, DateTimeOffset? expiry)>();
                    var dbUser = await db.Users
                    .Include(u => u.UserRoles)
                    .SingleOrDefaultAsync(x => x.UserId == userId.ConvertToUncheckedLong()).ConfigureAwait(false);
                foreach (var roles in dbUser.UserRoles)
                {
                    listOfRoles.Add((roles.Id, roles.RemovalAt));
                }
                return listOfRoles;
            }
        }

        public async Task FlushQueue()
        {
            await ActivityTimerTask().ConfigureAwait(false);
        }
    }

    public static class IdHelper
    {
        public static long ConvertToUncheckedLong(this ulong u)
        {
            unchecked { return (long)u; }
        }

        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }
    }
}

