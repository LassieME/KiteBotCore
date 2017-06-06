using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Utils;

namespace KiteBotCore.Modules.Rank
{
    [RequireContext(ContextType.Guild), RequireServer(Server.GiantBomb)]
    public class Ranks : ModuleBase
    {
        public RankService RankService { get; set; }

        [Command("ranks")]
        [Alias("colors", "colours")]
        [Summary("Shows you your current rank, based on the amount of time since you joined this server")]
        [RequireChannel(213359477162770433, 319557542562889729)]
        public async Task RanksCommand()
        {
            var sb = new StringBuilder("You currently have these ranks:\n");

            foreach (RankConfigs.GuildRanks.Rank rank in await RankService.GetAwardedRoles(Context.User as SocketGuildUser, Context.Guild).ConfigureAwait(false))
            {
                sb.AppendLine($"{Context.Guild.Roles.First(x => x.Id == rank.RoleId).Name} rewarded by {rank.RequiredTimeSpan.ToPrettyFormat()} in the server");
            }

            var (nextRole, timeSpan) = await RankService.GetNextRole(Context.User as SocketGuildUser, Context.Guild).ConfigureAwait(false);
            if (nextRole != null)
                sb.AppendLine($"Your next rank is {nextRole.Name} in another {timeSpan.ToPrettyFormat()}");

            var guildColors = (await RankService.GetAvailableColors(Context.User as SocketGuildUser, Context.Guild).ConfigureAwait(false)).ToList();
            if (guildColors.Any())
            {
                sb.Append($"You currently have these colors available: {string.Join(", ", Context.Guild.Roles.Where(x => guildColors.Contains(x.Id)).Select(x => x.Mention))}.");
            }
            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
        }

        [Command("rank")]
        [Alias("color", "colour")]
        [Summary("Select a rank available to you, as shown in the ranks command")]
        [RequireOwnerOrUserPermission(GuildPermission.ManageGuild)]
        [RequireChannel(213359477162770433, 319557542562889729)]
        public async Task RankCommand([Remainder] IRole role)
        {
            var availableColors = await RankService.GetAvailableColors(Context.User as SocketGuildUser, Context.Guild).ConfigureAwait(false);
            
            if (availableColors.Contains(role.Id))
            {
                var user = (IGuildUser) Context.User;
                //Check for any rank, since users can get demoted and then use this command before they get their grade back, might not be nessesary in the future
                ulong currentRank = RankService.GetRanksForGuild(Context.Guild.Id).SelectMany(x => x.Colors).FirstOrDefault(x => user.RoleIds.Contains(x));
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
            else
            {
                await ReplyAsync("You don't have access to that, yet").ConfigureAwait(false);
            }
        }

        [Command("rank test")]
        [Summary("Select a rank available to you, as shown in the ranks command")]
        [RequireOwnerOrUserPermission(GuildPermission.ManageGuild)]
        [RequireChannel(213359477162770433, 319557542562889729)]
        public async Task RankTestCommand()
        {
            var result = await RankService.AddAndRemoveMissingRanks(Context.User as SocketGuildUser, Context.Guild).ConfigureAwait(false);
            await ReplyAsync($"Add: {string.Join(", ", result.rolesToAdd)}\nRemove: {string.Join(", ", result.rolesToRemove)}").ConfigureAwait(false);
        }

        [Group("rank")]
        [RequireOwnerOrUserPermission(GuildPermission.Administrator), RequireContext(ContextType.Guild)]
        public class RankAdmin : ModuleBase
        {
            public RankService RankService { get; set; }

            [Command("enable")]
            public Task EnableCommand()
            {
                RankService.EnableGuildRanks(Context.Guild.Id);
                return Task.CompletedTask;
            }

            [Command("add")]
            [Summary("Adds a new Rank")]
            public async Task AddRankCommand(IRole role,
                [OverrideTypeReader(typeof(KiteTimeSpanReader))] TimeSpan timeSpan)
            {
                RankService.AddRank(Context.Guild.Id, role.Id, timeSpan);
                await ReplyAsync("OK").ConfigureAwait(false);
            }

            [Command("remove")]
            [Summary("Removes a new Rank")]
            public async Task RemoveRankCommand(IRole role)
            {
                var remove = RankService.RemoveRank(Context.Guild.Id, role.Id);
                await ReplyAsync(remove ? "OK" : "Couldn't find rank").ConfigureAwait(false);
            }

            [Command("list")]
            [Summary("Lists all ranks and their potential associated colors")]
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
        [RequireOwnerOrUserPermission(GuildPermission.Administrator), RequireContext(ContextType.Guild)]
        public class Color : ModuleBase
        {
            public RankService RankService { get; set; }

            [Command("add")]
            [Summary("Attaches a color role to a rank role")]
            public async Task AddColorCommand(IRole roleRank, IRole color)
            {
                RankService.AddColorToRank(Context.Guild.Id, roleRank.Id, color.Id);
                await ReplyAsync("OK").ConfigureAwait(false);
            }

            [Command("remove")]
            [Summary("Removes a color from a rank")]
            public async Task RemoveRankCommand(IRole rankRole, IRole colorRole)
            {
                var remove = RankService.RemoveColorFromRank(Context.Guild.Id, rankRole.Id, colorRole);
                await ReplyAsync(remove ? "OK" : "Couldn't remove rank").ConfigureAwait(false);
            }
        }
    }
}
