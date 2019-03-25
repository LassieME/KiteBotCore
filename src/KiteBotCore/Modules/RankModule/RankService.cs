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
    public class RankService : IRankService
    {
        private readonly DiscordContextFactory _discordFactory;
        private readonly DiscordSocketClient _client;
        private readonly RankConfigs _rankConfigs;
        private readonly Action<RankConfigs> _saveFunc;
        private readonly ConcurrentQueue<(IGuildUser user, DateTimeOffset lastActivityAt)> _activityQueue;
        private readonly ConcurrentQueue<(IGuildUser user, IEnumerable<ulong> rolesToAdd, IEnumerable<ulong> rolesToRemove)> _roleUpdateQueue;
        private readonly Timer _roleTimer;
        private readonly Timer _activityTimer;
        private readonly List<IRoleProvider> _roleProviders;

        public RankService(RankConfigs rankConfigs, Action<RankConfigs> saveFunc, DiscordContextFactory discordContextFactory, DiscordSocketClient client)
        {
            _discordFactory = discordContextFactory;
            _client = client;
            _rankConfigs = rankConfigs;
            _saveFunc = saveFunc;
            _activityQueue = new ConcurrentQueue<(IGuildUser, DateTimeOffset)>();
            _roleUpdateQueue = new ConcurrentQueue<(IGuildUser user, IEnumerable<ulong> rolesToAdd, IEnumerable<ulong> rolesToRemove)>();

            client.UserJoined += AddUserTask;
            client.MessageReceived += UpdateLastActivityTask;

            _roleTimer = new Timer(async e => await Task.Run(async () => await RoleTimerTask().ConfigureAwait(false)).ConfigureAwait(false), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));

            _activityTimer = new Timer(async e => await Task.Run(async () => await ActivityTimerTask().ConfigureAwait(false)).ConfigureAwait(false), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            _roleProviders = new List<IRoleProvider> { new AssignedRoleProvider(), new RankRoleProvider(rankConfigs) };
        }

        //TODO: Should probably not be here
        private async Task<User> AddUserTask(IGuildUser userInput)
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

        private Task UpdateLastActivityTask(SocketMessage message)
        {
            if (message.Channel is SocketGuildChannel messageGuildChannel && message.Author is SocketGuildUser messageAuthor && _rankConfigs.GuildConfigs.ContainsKey(messageGuildChannel.Guild.Id))
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

                                var joinDate = dbUser.JoinedAt;
                                var activityDate = dbUser.LastActivityAt;

                                List<IRankRole> ranks = await GetAwardedRolesAsync(socketGuildUser, guild,
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

                                foreach (IRoleProvider roleProvider in _roleProviders)
                                {
                                    List<IRankRole> list = await roleProvider.GetUserRoles(socketGuildUser, db);
                                    ranks.AddRange(list);
                                }

                                await AddAndRemoveRanksAsync(socketGuildUser, guild, ranks, assignedColorsToRemove)
                                    .ConfigureAwait(false);


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

        internal async Task AddAndRemoveRanksAsync(IGuildUser messageAuthor, IGuild guild, IList<IRankRole> result, IList<UserColorRoles> userColorToRemove)
        {
            if (result == null)
                result = (await GetAwardedRolesAsync(messageAuthor, guild).ConfigureAwait(false)).ToList();

            //Find any roles that a person has a claim to
            var missingRoles = result
                .Where(x => messageAuthor.RoleIds.All(y => y != x.Id))
                .Select(x => x.Id)
                .ToArray();

            //Find any roles currently applied to remove due to changing circumstances
            var rolesToRemove = messageAuthor.RoleIds.Where(x =>
                    GetRanksForGuild(guild)
                    .Select(y => y.Id)
                    .Contains(x) && !result.Select(y => y.Id)
                    .Contains(x));

            var colors = result.SelectMany(x => x.Colors).Select(y => y.Id);
            var allColorIds = GetRanksForGuild(guild).SelectMany(x => x.Colors).Select(y => y.Id);
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

        public async Task<List<IRankRole>> GetAwardedRolesAsync(IGuildUser user, IGuild guild, DateTimeOffset joinDate = default, DateTimeOffset lastActivity = default)
        {
            if (joinDate == default)
                joinDate = await GetUserJoinDateAsync(user, guild).ConfigureAwait(false);
            if (lastActivity == default)
                lastActivity = await GetUserLastActivityAsync(user, guild).ConfigureAwait(false);
            var timeInGuild = DateTimeOffset.UtcNow - joinDate;
            var allRanksInGuild = GetRanksForGuild(guild).ToList();

            return allRanksInGuild
                .Where(x => x.RequiredTimeSpan < timeInGuild)
                .Take(allRanksInGuild
                    .Where(x => x.RequiredTimeSpan < timeInGuild).ToList()
                    .Count - (DateTimeOffset.UtcNow - lastActivity).Days / 7).ToList();
        }

        public async Task<DateTimeOffset> GetUserLastActivityAsync(IGuildUser inputUser, IGuild guild)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var user = await db.FindAsync<User>(inputUser.Id.ConvertToUncheckedLong()).ConfigureAwait(false);

                Debug.Assert(user != null);
                Debug.Assert(user.LastActivityAt != null, "user.LastActivityAt != null");
                return user.LastActivityAt;
            }
        }

        public async Task<DateTimeOffset> GetUserJoinDateAsync(IUser inputUser, IGuild guild)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var user = await db.FindAsync<User>(inputUser.Id.ConvertToUncheckedLong()).ConfigureAwait(false);

                Debug.Assert(user != null);
                Debug.Assert(user.JoinedAt != null, "user.JoinedAt != null");
                return user.JoinedAt.Value;
            }
        }

        public async Task<IEnumerable<IColor>> GetAvailableColorsAsync(IGuildUser user, IGuild guild)
        {
            //Gets regular colors awarded by reaching ranks
            var awardedRoles = (await GetAwardedRolesAsync(user, guild).ConfigureAwait(false)).SelectMany(x => x.Colors);
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

        public async Task<(Discord.IRole role, TimeSpan timeToRole)> GetNextRoleAsync(IGuildUser user, IGuild guild)
        {
            var timeInGuild = DateTimeOffset.UtcNow - await GetUserJoinDateAsync(user, guild).ConfigureAwait(false);
            var nextRank = _rankConfigs.GuildConfigs[guild.Id].Ranks.Values.FirstOrDefault(y => y.RequiredTimeSpan > timeInGuild);
            var guildRole = nextRank != null ? guild.Roles.FirstOrDefault(x => x.Id == nextRank.Id) : null;
            return (guildRole, nextRank != null ? nextRank.RequiredTimeSpan - timeInGuild : TimeSpan.MaxValue);
        }

        public void AddRank(IGuild guild, Discord.IRole role, TimeSpan timeSpan)
        {
            if (_rankConfigs.GuildConfigs.TryGetValue(guild.Id, out var ranks))
                ranks.Ranks.Add(role.Id, new Rank
                {
                    Id = role.Id,
                    Colors = new List<Color>(),
                    RequiredTimeSpan = timeSpan
                });
            else
                _rankConfigs.GuildConfigs.Add(guild.Id, new GuildRanks
                {
                    GuildId = guild.Id,
                    Ranks = new Dictionary<ulong, Rank>
                    {
                        {
                            role.Id,
                            new Rank
                            {
                                Id = role.Id,
                                Colors = new List<Color>(),
                                RequiredTimeSpan = timeSpan
                            }
                        }
                    }
                });
            _saveFunc(_rankConfigs);
        }

        public bool RemoveRank(IGuild guild, Discord.IRole role)
        {
            var result = _rankConfigs.GuildConfigs[guild.Id].Ranks.Remove(role.Id);
            if (result)
                _saveFunc(_rankConfigs);
            return result;
        }

        public IEnumerable<IRankRole> GetRanksForGuild(IGuild guild)
        {
            return _rankConfigs.GuildConfigs[guild.Id].Ranks.Values;
        }

        public void AddColorToRank(IGuild guild, Discord.IRole roleRank, Discord.IRole color)
        {
            var configColor = new Color { Id = color.Id };
            _rankConfigs.GuildConfigs[guild.Id].Ranks[roleRank.Id].Colors.Add(configColor);
            _saveFunc(_rankConfigs);
        }

        public bool RemoveColorFromRank(IGuild guild, Discord.IRole rankRole, Discord.IRole colorRole)
        {
            var colorToRemove = _rankConfigs.GuildConfigs[guild.Id].Ranks[rankRole.Id].Colors
                .FirstOrDefault(x => x.Id == colorRole.Id);
            var result = _rankConfigs.GuildConfigs[guild.Id].Ranks[rankRole.Id].Colors.Remove(colorToRemove);
            _saveFunc(_rankConfigs);
            return result;
        }

        public void EnableGuildRanks(IGuild guild)
        {
            _rankConfigs.GuildConfigs.Add(guild.Id, new GuildRanks
            {
                GuildId = guild.Id,
                Ranks = new Dictionary<ulong, Rank>()
            });
            _saveFunc(_rankConfigs);
        }

        public async Task SetJoinDateAsync(IUser user, IGuild guild, DateTimeOffset newDate)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var dbUser = await db.Users.FindAsync(user.Id.ConvertToUncheckedLong()).ConfigureAwait(false);
                dbUser.JoinedAt = newDate;
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<bool> AssignColorToUserAsync(IGuild guild, IUser user, Discord.IRole colorRole, TimeSpan? timeToRemove)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var dbUser = await db.Users
                    .Include(u => u.UserRoles)
                    .SingleOrDefaultAsync(x => x.Id == user.Id).ConfigureAwait(false);
                if (dbUser.UserRoles == null)
                {
                    dbUser.UserRoles = new List<UserColorRoles>();
                }

                if (dbUser.UserRoles.Any(x => x.Id == colorRole.Id))
                {
                    return false;
                }
                dbUser.UserRoles.Add(new UserColorRoles()
                {
                    Id = colorRole.Id,
                    RemovalAt = timeToRemove == null ? null : DateTimeOffset.UtcNow + timeToRemove,
                    uId = dbUser.Id,
                    User = dbUser
                });
                await db.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
        }

        public async Task<bool> UnassignColorFromUserAsync(IUser user, Discord.IRole rankRole)
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

        public async Task<IList<(ulong roleId, DateTimeOffset? expiry)>> GetAssignedRolesAsync(IGuild guild, IUser user)
        {
            using (var db = _discordFactory.Create(new DbContextFactoryOptions()))
            {
                var listOfRoles = new List<(ulong roleId, DateTimeOffset? expiry)>();
                var dbUser = await db.Users
                .Include(u => u.UserRoles)
                .SingleOrDefaultAsync(x => x.UserId == user.Id.ConvertToUncheckedLong()).ConfigureAwait(false);
                foreach (var roles in dbUser.UserRoles)
                {
                    listOfRoles.Add((roles.Id, roles.RemovalAt));
                }
                return listOfRoles;
            }
        }

        public async Task FlushQueueAsync()
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

