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
            bool debug = true;
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
                sb.AppendLine($"You currently have these colors available: {string.Join(", ", Context.Guild.Roles.Where(x => guildColors.Contains(x.Id)).OrderBy(x => x.Position).Select(x => x.Mention))}.");
            }
            if (debug)
            {
                sb.AppendLine($"debug info: last activity at {await RankService.GetUserLastActivity(Context.User as SocketGuildUser, Context.Guild).ConfigureAwait(false)}");
                sb.AppendLine($"Joindate used : {await RankService.GetUserJoinDate(Context.User as SocketGuildUser,Context.Guild).ConfigureAwait(false)}");
            }
            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
        }

        [Command("ranks")]
        [Alias("colors", "colours")]
        [Summary("Shows you your current rank, based on the amount of time since you joined this server")]
        [RequireChannel(213359477162770433, 319557542562889729), RequireOwnerOrUserPermission(GuildPermission.Administrator)]
        public async Task RanksCommand(IUser user)
        {
            bool debug = true;
            var sb = new StringBuilder("You currently have these ranks:\n");

            foreach (RankConfigs.GuildRanks.Rank rank in await RankService.GetAwardedRoles(user as SocketGuildUser, Context.Guild).ConfigureAwait(false))
            {
                sb.AppendLine($"{Context.Guild.Roles.First(x => x.Id == rank.RoleId).Name} rewarded by {rank.RequiredTimeSpan.ToPrettyFormat()} in the server");
            }

            var (nextRole, timeSpan) = await RankService.GetNextRole(user as SocketGuildUser, Context.Guild).ConfigureAwait(false);
            if (nextRole != null)
                sb.AppendLine($"Your next rank is {nextRole.Name} in another {timeSpan.ToPrettyFormat()}");

            var guildColors = (await RankService.GetAvailableColors(user as SocketGuildUser, Context.Guild).ConfigureAwait(false)).ToList();
            if (guildColors.Any())
            {
                sb.AppendLine($"You currently have these colors available: {string.Join(", ", Context.Guild.Roles.Where(x => guildColors.Contains(x.Id)).Select(x => x.Mention))}.");
            }
            if (debug)
            {
                sb.AppendLine($"debug info: last activity recorded at {await RankService.GetUserLastActivity(user as SocketGuildUser, Context.Guild).ConfigureAwait(false)}");
            }
            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
        }

        [Command("rank")]
        [Alias("color", "colour")]
        [Summary("Select a rank available to you, as shown in the ranks command")]
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

        

        [Group("rank")]
        [RequireContext(ContextType.Guild)]
        public class RankAdmin : ModuleBase
        {
            public RankService RankService { get; set; }

            [Command("enable")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator)]
            public Task EnableCommand()
            {
                RankService.EnableGuildRanks(Context.Guild.Id);
                return Task.CompletedTask;
            }

            [Command("add")]
            [Summary("Adds a new Rank")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator)]
            public async Task AddRankCommand(IRole role,
                [OverrideTypeReader(typeof(KiteTimeSpanReader))] TimeSpan timeSpan)
            {
                RankService.AddRank(Context.Guild.Id, role.Id, timeSpan);
                await ReplyAsync("OK").ConfigureAwait(false);
            }

            [Command("remove")]
            [Summary("Removes a new Rank")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator)]
            public async Task RemoveRankCommand(IRole role)
            {
                var remove = RankService.RemoveRank(Context.Guild.Id, role.Id);
                await ReplyAsync(remove ? "OK" : "Couldn't find rank").ConfigureAwait(false);
            }

            [Command("setjoindate")]
            [Summary("Sets a new join date for a user by channelId and messageId")]
            [RequireOwnerOrUserPermission(GuildPermission.Administrator)]
            public async Task SetJoinDate(IUser user, IChannel channel, ulong messageId)
            {
                var message = await (channel as ITextChannel).GetMessageAsync(messageId).ConfigureAwait(false);
                if (message.Author.Id == user.Id)
                {
                    await RankService.SetJoinDate(user, Context.Guild.Id, message.Timestamp).ConfigureAwait(false);
                    await ReplyAsync("New date set, try the ranks command again.").ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync("Message given was not made by the given user.").ConfigureAwait(false);
                }
            }

            [Command("setjoindate")]
            [Summary("Sets a new join date for a user by channelId and messageId")]
            public async Task SetJoinDate(IChannel channel, ulong messageId)
            {
                var message = await (channel as ITextChannel).GetMessageAsync(messageId).ConfigureAwait(false);
                if (message.Author.Id == Context.User.Id)
                {
                    await RankService.SetJoinDate(Context.User, Context.Guild.Id, message.Timestamp).ConfigureAwait(false);
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
                var list = RankService.GetRanksForGuild(Context.Guild.Id);
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("```");
                foreach (RankConfigs.GuildRanks.Rank rank in list)
                {
                    stringBuilder.Append(Context.Guild.Roles.First(x => x.Id == rank.RoleId).Name)
                        .AppendLine($" => {rank.RequiredTimeSpan.ToPrettyFormat()}");
                    foreach (ulong rankId in rank.Colors)
                    {
                        stringBuilder.AppendLine($"\t{Context.Guild.Roles.First(x => x.Id == rankId).Name}");
                    }
                }
                stringBuilder.Append("```");
                await ReplyAsync(stringBuilder.ToString()).ConfigureAwait(false);
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
