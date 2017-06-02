using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using KiteBotCore.Utils;

namespace KiteBotCore.Modules.Rank
{
    public class Ranks : ModuleBase
    {
        public RankService RankService { get; set; }

        [Command("ranks")]
        [Summary("Shows you your current rank, based on the amount of time since you joined this server")]
        [RequireContext(ContextType.Guild), RequireServer(Server.GiantBomb)]
        public async Task RanksCommand()
        {
            var result = await RankService.CheckUserJoinDate(Context.User, Context.Guild).ConfigureAwait(false);
            var timeInGuild = DateTimeOffset.UtcNow - result;
            var sb = new StringBuilder("You currently have these ranks:\n");

            foreach (RankConfigs.GuildRanks.Rank rank in RankService.GetRanksForGuild(Context.Guild.Id).Where(x => x.RequiredTimeSpan < timeInGuild))
            {
                sb.AppendLine($"{Context.Guild.Roles.First(x => x.Id == rank.RoleId).Name} rewarded by {rank.RequiredTimeSpan.ToPrettyFormat()} in the server");
            }

            if (RankService.GetRanksForGuild(Context.Guild.Id).Any(x => x.RequiredTimeSpan > timeInGuild))
            {
                var guildRanks = RankService.GetRanksForGuild(Context.Guild.Id);
                var nextRank = guildRanks.FirstOrDefault(y => y.RequiredTimeSpan > timeInGuild);
                var nextRole = Context.Guild.Roles.FirstOrDefault(x => nextRank.RoleId == x.Id);
                sb.AppendLine(
                    $"Your next rank is {nextRole.Name} in another {(nextRank.RequiredTimeSpan - timeInGuild).ToPrettyFormat()}");
            }

            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
        }

        [Command("rank")]
        [Summary("Select a rank available to you, as shown in the ranks command")]
        [RequireContext(ContextType.Guild), RequireServer(Server.GiantBomb), RequireOwnerOrUserPermission(GuildPermission.ManageGuild)]
        public async Task RankCommand([Remainder] IRole role)
        {
            var result = await RankService.CheckUserJoinDate(Context.User, Context.Guild).ConfigureAwait(false);
            var timeInGuild = DateTimeOffset.UtcNow - result;
            var availableRanks = RankService.GetRanksForGuild(Context.Guild.Id);
            var availableColors = availableRanks.Where(x => x.RequiredTimeSpan < timeInGuild).SelectMany(x => x.Colors).ToList();

            if (availableColors.Contains(role.Id))
            {
                var user = (IGuildUser) Context.User;
                ulong currentRank = availableColors.FirstOrDefault(x => user.RoleIds.Contains(x));
                if (currentRank != 0)
                {
                    if (currentRank == role.Id)
                    {
                        await user.RemoveRoleAsync(role).ConfigureAwait(false);
                        await ReplyAsync("Removed existing rank").ConfigureAwait(false);
                        return;
                    }
                    await user.RemoveRoleAsync(Context.Guild.Roles.First(x => x.Id == currentRank)).ConfigureAwait(false);
                }
                await user.AddRoleAsync(role).ConfigureAwait(false);
                await ReplyAsync($"Added {role.Name}").ConfigureAwait(false);
            }
        }

        [Group("rank")]
        public class RankAdmin : ModuleBase
        {
            public RankService RankService { get; set; }

            [Command("enable")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator), RequireContext(ContextType.Guild)]
            public Task EnableCommand()
            {
                RankService.EnableGuildRanks(Context.Guild.Id);
                return Task.CompletedTask;
            }

            [Command("add")]
            [Summary("Adds a new Rank")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator), RequireContext(ContextType.Guild)]
            public async Task AddRankCommand(IRole role,
                [OverrideTypeReader(typeof(KiteTimeSpanReader))] TimeSpan timeSpan)
            {
                RankService.AddRank(Context.Guild.Id, role.Id, timeSpan);
                await ReplyAsync("OK").ConfigureAwait(false);
            }

            [Command("remove")]
            [Summary("Removes a new Rank")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator), RequireContext(ContextType.Guild)]
            public async Task RemoveRankCommand(IRole role)
            {
                var remove = RankService.RemoveRank(Context.Guild.Id, role.Id);
                await ReplyAsync(remove ? "OK" : "Couldn't find rank").ConfigureAwait(false);
            }

            [Command("list")]
            [Summary("Lists all ranks and their potential associated colors")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator), RequireContext(ContextType.Guild)]
            public async Task ListRankCommand()
            {
                var list = RankService.GetRanksForGuild(Context.Guild.Id);
                var stringbuilder = new StringBuilder();
                stringbuilder.Append("```");
                foreach (RankConfigs.GuildRanks.Rank rank in list)
                {
                    stringbuilder.Append(Context.Guild.Roles.First(x => x.Id == rank.RoleId).Name)
                        .AppendLine($" => {rank.RequiredTimeSpan.ToPrettyFormat()}");
                    foreach (ulong rankId in rank.Colors)
                    {
                        stringbuilder.AppendLine($"\t{Context.Guild.Roles.First(x => x.Id == rankId).Name}");
                    }
                }
                stringbuilder.Append("```");
                await ReplyAsync(stringbuilder.ToString()).ConfigureAwait(false);
            }
        }

        [Group("color")]
        public class Color : ModuleBase
        {
            public RankService RankService { get; set; }

            [Command("add")]
            [Summary("Attaches a color role to a rank role")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator), RequireContext(ContextType.Guild)]
            public async Task AddColorCommand(IRole roleRank, IRole color)
            {
                RankService.AddColorToRank(Context.Guild.Id, roleRank.Id, color.Id);
                await ReplyAsync("OK").ConfigureAwait(false);
            }

            [Command("remove")]
            [Summary("Removes a color from a rank")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator), RequireContext(ContextType.Guild)]
            public async Task RemoveRankCommand(IRole rankRole, IRole colorRole)
            {
                var remove = RankService.RemoveColorFromRank(Context.Guild.Id, rankRole.Id, colorRole);
                await ReplyAsync(remove ? "OK" : "Couldn't remove rank").ConfigureAwait(false);
            }
        }
    }
}
