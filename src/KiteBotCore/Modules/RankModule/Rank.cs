using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using KiteBotCore.Utils;
using Microsoft.AspNetCore.Hosting.Internal;

namespace KiteBotCore.Modules.RankModule
{
    [RequireContext(ContextType.Guild), RequireServer(Server.GiantBomb)]
    public class Ranks : ModuleBase
    {
        public enum Debug
        {
            Debug,
            Release
        }

        public IRankService RankService { get; set; }

        public KiteBotDbContext DbContext { get; set; }

        //[Command("opt-out", RunMode = RunMode.Sync)]
        //[Summary("Opts out of the entire rank system and all data collection for this bot")]
        //[RequireChannel(213359477162770433)]
        //public async Task OptOutCommand()
        //{
        //    using (var db = DbContext)
        //    {
        //        var user = await db.FindAsync<User>(Context.User.Id.ConvertToUncheckedLong())
        //            .ConfigureAwait(false);
        //        if (user.OptOut == false)
        //        {
        //            user.OptOut = true;
        //            user.LastActivityAt = default(DateTimeOffset);
        //            await db.SaveChangesAsync().ConfigureAwait(false);
        //            await ReplyAsync("👌😢").ConfigureAwait(false);
        //        }
        //        else
        //        {
        //            await ReplyAsync("You're already opted out, use ~opt-in to opt back in").ConfigureAwait(false);
        //        }
        //    }
        //}

        //[Command("opt-in", RunMode = RunMode.Sync)]
        //[Summary("Opts in to the greatness that is the Mister GBot ranking system")]
        //[RequireChannel(213359477162770433)]
        //public async Task OptInCommand()
        //{
        //    using (var db = DbContext)
        //    {
        //        var user = await db.FindAsync<User>(Context.User.Id.ConvertToUncheckedLong())
        //            .ConfigureAwait(false);
        //        if (user.OptOut)
        //        {
        //            user.OptOut = false;
        //            user.LastActivityAt = DateTimeOffset.UtcNow;
        //            await db.SaveChangesAsync().ConfigureAwait(false);
        //            await ReplyAsync("👌😁").ConfigureAwait(false);
        //        }
        //        else
        //        {
        //            await ReplyAsync("You're already opted in, use ~opt-out to GET OUT NOW!").ConfigureAwait(false);
        //        }
        //    }
        //}

        //[Command("premium", RunMode = RunMode.Async)]
        //[Summary("Links to the page where you can add premium")]
        //[RequireChannel(213359477162770433)]
        //public async Task PremiumCommand(Debug showDebugInfo = Debug.Release)
        //{
        //    await ReplyAsync("https://lassie.me/GBot/LinkAccounts").ConfigureAwait(false);
        //}

        [Command("ranks", RunMode = RunMode.Async)]
        [Alias("colors", "colours")]
        [Summary("Shows you your current rank, based on the amount of time since you joined this server")]
        [RequireChannel(213359477162770433)]
        public Task RanksCommand(Debug showDebugInfo = Debug.Release) =>
            RanksCommand(Context.User, showDebugInfo);

        [Command("ranks", RunMode = RunMode.Async)]
        [Alias("colors", "colours")]
        [Summary("Shows you your current rank, based on the amount of time since you joined this server")]
        [RequireChannel(213359477162770433)]
        public async Task RanksCommand(IUser user, Debug showDebugInfo = Debug.Release)
        {
            await RankService.FlushQueueAsync().ConfigureAwait(false);
            var embed = new EmbedBuilder();

            var userRanks = await RankService.GetAwardedRolesAsync(user as SocketGuildUser, Context.Guild).ConfigureAwait(false);
            if (userRanks.Count > 0)
            {
                var sb = new StringBuilder("You currently have these ranks:\n");
                foreach (var rank in userRanks)
                {
                    sb.Append(Context.Guild.Roles.First(x => x.Id == rank.Id).Name).Append(" rewarded by ").Append(rank.RequiredTimeSpan.ToPrettyFormat()).AppendLine(" in the server");
                }
                embed.AddField(x =>
                {
                    x.Name = "Ranks";
                    x.IsInline = true;
                    x.Value = sb.ToString();
                });
            }
            else
            {
                embed.AddField(x =>
                {
                    x.Name = "Ranks";
                    x.IsInline = true;
                    x.Value = "You currently have no ranks";
                });
            }

            var (nextRole, timeSpan) = await RankService.GetNextRoleAsync(user as SocketGuildUser, Context.Guild).ConfigureAwait(false);
            if (nextRole != null)
            {
                embed.AddField(x =>
                {
                    x.Name = "Next Rank";
                    x.IsInline = true;
                    x.Value = $"Your next rank is {nextRole.Name} in another {timeSpan.ToPrettyFormat()}";
                });
            }

            var assignedRoles = await RankService.GetAssignedRolesAsync(Context.Guild, user).ConfigureAwait(false);
            if (assignedRoles.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var role in assignedRoles)
                {
                    var name = Context.Guild.Roles.First(x => x.Id == role.roleId).Name;
                    var expiry = role.expiry;
                    sb.AppendLine(expiry == null
                        ? name : $"{name} expires in {(expiry - DateTimeOffset.UtcNow).Value.ToPrettyFormat()}");
                }
                embed.AddField(x =>
                {
                    x.Name = "Assigned Roles";
                    x.IsInline = true;
                    x.Value = sb.ToString();
                });
            }

            var guildColors = (await RankService.GetAvailableColorsAsync(user as SocketGuildUser, Context.Guild).ConfigureAwait(false)).ToList();
            if (guildColors.Count > 0)
            {
                var roles = Context.Guild.Roles.Where(x => guildColors.Any(y => y.Id == x.Id)).OrderBy(x => x.Position);
                embed.AddField(e =>
                {
                    e.Name = "Colors";
                    e.IsInline = true;
                    e.Value =
                        $"You currently have these colors available:\n {string.Join(", ", roles.Select(x => x.Mention))}.";
                });
                embed.WithColor(roles.Last().Color);
            }

            if (showDebugInfo == Debug.Debug)
            {
                string info = $"Last activity: {await RankService.GetUserLastActivityAsync(user as SocketGuildUser, Context.Guild).ConfigureAwait(false)}\n" +
                              $"Joindate used: {await RankService.GetUserJoinDateAsync(user as SocketGuildUser, Context.Guild).ConfigureAwait(false)}";
                embed.AddField(e =>
                {
                    e.Name = "Debug info";
                    e.IsInline = true;
                    e.Value = info;
                });
            }

            embed.WithAuthor(x =>
            {
                var url = user.GetAvatarUrl();
                if(url != null)
                    x.IconUrl = url;
                x.Name = (user as SocketGuildUser)?.Nickname ?? user.Username;
            });

            await ReplyAsync("", false, embed.Build()).ConfigureAwait(false);
        }

        [Command("rank", RunMode = RunMode.Async), Priority(2)]
        [Alias("color", "colour")]
        [Summary("Select a rank available to you, as shown in the ranks command")]
        [RequireChannel(213359477162770433)]
        public Task RankCommand() => RanksCommand(Context.User);

        [Command("rank", RunMode = RunMode.Async), Priority(2)]
        [Alias("color", "colour")]
        [Summary("Select a rank available to you, as shown in the ranks command")]
        [RequireChannel(213359477162770433)]
        public Task RankCommand(Discord.IRole role) => RankCommand(Context.User as IGuildUser, role);

        [Command("rank"),Priority(3)]
        [Alias("color", "colour")]
        [Summary("Select a rank available to you, as shown in the ranks command")]
        [RequireChannel(213359477162770433), RequireOwnerOrUserPermission(GuildPermission.Administrator)]
        public async Task RankCommand(IGuildUser user, [Remainder] Discord.IRole role)
        {
            var availableColors = (await RankService.GetAvailableColorsAsync(user, Context.Guild).ConfigureAwait(false)).ToList();

            if (RankService.GetRanksForGuild(Context.Guild).SelectMany(x => x.Colors).Select(x => x.Id).Union(availableColors.Select(x => x.Id)).All(y => y != role.Id))
            {
                await ReplyAsync("That role is not managed by this bot, try `?rank`").ConfigureAwait(false);
                return;
            }
            if (availableColors.Any(x => x.Id == role.Id))
            {
                //Check for any rank, since users can get demoted and then use this command before they get their grade back, might not be nessesary in the future
                var currentRank = RankService.GetRanksForGuild(Context.Guild)
                    .SelectMany(x => x.Colors)
                    .Union(availableColors)
                    .FirstOrDefault(x => user.RoleIds.Any(y => y == x.Id));

                if (currentRank != null)
                {
                    if (currentRank.Id == role.Id)
                    {
                        await user.RemoveRoleAsync(role).ConfigureAwait(false);
                        await ReplyAsync("Removed existing rank").ConfigureAwait(false);
                        return;
                    }
                    await user.RemoveRoleAsync(Context.Guild.Roles.First(x => x.Id == currentRank.Id)).ConfigureAwait(false);
                }
                await user.AddRoleAsync(role).ConfigureAwait(false);
                await ReplyAsync($"Added {role.Name}").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"You don't have access to that, {Format.Italics("yet...")}").ConfigureAwait(false);
            }
        }

        [Command("assign")]
        [Summary("assign an individual a color with an optional expiration")]
        [RequireOwnerOrUserPermission(GuildPermission.ManageGuild), RequireContext(ContextType.Guild)]
        public async Task AssignColorCommand(IUser user, Discord.IRole colorRankRole, [OverrideTypeReader(typeof(KiteTimeSpanReader))] TimeSpan timeToRemove = default)
        {
            var timeSpan = timeToRemove != default ? (TimeSpan?)timeToRemove : null;
            var assignTask = await RankService.AssignColorToUserAsync(Context.Guild, user, colorRankRole, timeSpan).ConfigureAwait(false);
            await ReplyAsync(assignTask ? "OK" : "Couldn't remove rank").ConfigureAwait(false);
        }

        [Command("unassign"), Alias("deassign")]
        [Summary("assign an individual a color with an optional expiration")]
        [RequireOwnerOrUserPermission(GuildPermission.ManageGuild), RequireContext(ContextType.Guild)]
        public async Task UnassignColorCommand(IUser user, Discord.IRole rankRole)
        {
            var result = await RankService.UnassignColorFromUserAsync(user, rankRole).ConfigureAwait(false);
            await ReplyAsync(result ? "OK" : "Couldn't remove rank").ConfigureAwait(false);
        }

        [Group("rank")]
        [RequireContext(ContextType.Guild)]
        public class RankAdmin : ModuleBase
        {
            public IRankService RankService { get; set; }

            [Command("enable")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator)]
            public Task EnableCommand()
            {
                RankService.EnableGuildRanks(Context.Guild);
                return Task.CompletedTask;
            }

            [Command("add")]
            [Summary("Adds a new Rank")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator)]
            public async Task AddRankCommand(Discord.IRole rankRole,
                [OverrideTypeReader(typeof(KiteTimeSpanReader))] TimeSpan timeSpan)
            {
                RankService.AddRank(Context.Guild, rankRole, timeSpan);
                await ReplyAsync("OK").ConfigureAwait(false);
            }

            [Command("remove")]
            [Summary("Removes a new Rank")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator)]
            public async Task RemoveRankCommand(Discord.IRole rankRole)
            {
                var remove = RankService.RemoveRank(Context.Guild, rankRole);
                await ReplyAsync(remove ? "OK" : "Couldn't find rank").ConfigureAwait(false);
            }

            [Command("setjoindate")]
            [Summary("Sets a new join date for a user by channelId and messageId")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator)]
            public async Task SetJoinDate(IUser user, ITextChannel channel, ulong messageId)
            {
                var message = await channel.GetMessageAsync(messageId).ConfigureAwait(false);
                if (message.Author.Id == user.Id)
                {
                    await RankService.SetJoinDateAsync(user, Context.Guild, message.Timestamp).ConfigureAwait(false);
                    await ReplyAsync("New date set, try the ranks command again.").ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync("Message given was not made by the given user.").ConfigureAwait(false);
                }
            }

            [Command("setjoindate")]
            [Summary("Sets a new join date for a user by channelId and messageId")]
            public async Task SetJoinDate(ITextChannel channel, ulong messageId)
            {
                var message = await channel.GetMessageAsync(messageId).ConfigureAwait(false);
                if (message.Author.Id == Context.User.Id)
                {
                    await RankService.SetJoinDateAsync(Context.User, Context.Guild, message.Timestamp).ConfigureAwait(false);
                    await ReplyAsync("New date set, try the ranks command again.").ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync("Message given was not made by you").ConfigureAwait(false);
                }
            }

            [Command("list"), Priority(1), Summary("Lists all ranks and their potential associated colors")]
            public async Task ListRankCommand()
            {
                var list = RankService.GetRanksForGuild(Context.Guild);
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("```");
                foreach (var rank in list)
                {
                    stringBuilder.Append(Context.Guild.Roles.First(x => x.Id == rank.Id).Name)
                        .Append(" => ").AppendLine(rank.RequiredTimeSpan.ToPrettyFormat());
                    foreach (Json.Color color in rank.Colors)
                    {
                        stringBuilder.Append("\t").AppendLine(Context.Guild.Roles.First(x => x.Id == color.Id).Name);
                    }
                }
                stringBuilder.Append("```");
                await ReplyAsync(stringBuilder.ToString()).ConfigureAwait(false);
            }
        }

        [Group("color"),Alias("colour")]
        [RequireOwnerOrUserPermission(GuildPermission.Administrator), RequireContext(ContextType.Guild)]
        public class Color : ModuleBase
        {
            public IRankService RankService { get; set; }

            [Command("add")]
            [Summary("Attaches a color role to a rank role")]
            public async Task AddColorCommand(Discord.IRole rankRoleRank, Discord.IRole color)
            {
                RankService.AddColorToRank(Context.Guild, rankRoleRank, color);
                await ReplyAsync("OK").ConfigureAwait(false);
            }

            [Command("remove")]
            [Summary("Removes a color from a rank")]
            public async Task RemoveRankCommand(Discord.IRole rankRankRole, Discord.IRole colorRankRole)
            {
                var remove = RankService.RemoveColorFromRank(Context.Guild, rankRankRole, colorRankRole);
                await ReplyAsync(remove ? "OK" : "Couldn't remove rank").ConfigureAwait(false);
            }
        }
    }
}
