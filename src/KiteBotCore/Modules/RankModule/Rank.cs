using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.InteractiveCommands;
using KiteBotCore.Utils;

namespace KiteBotCore.Modules.RankModule
{
    [RequireContext(ContextType.Guild), RequireServer(Server.GiantBomb)]
    public class Ranks : InteractiveModuleBase
    {
        public enum Debug
        {
            Debug,
            Release
        }

        public RankService RankService { get; set; }

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
            await RankService.FlushQueue().ConfigureAwait(false);
            var embed = new EmbedBuilder();
            
            var userRanks = await RankService.GetAwardedRoles(user as SocketGuildUser, Context.Guild).ConfigureAwait(false);
            if (userRanks.Any())
            {
                var sb = new StringBuilder("You currently have these ranks:\n");
                foreach (Json.Rank rank in userRanks)
                {
                    sb.AppendLine(
                        $"{Context.Guild.Roles.First(x => x.Id == rank.RoleId).Name} rewarded by {rank.RequiredTimeSpan.ToPrettyFormat()} in the server");
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

            var (nextRole, timeSpan) = await RankService.GetNextRole(user as SocketGuildUser, Context.Guild).ConfigureAwait(false);
            if (nextRole != null)
            {
                embed.AddField(x =>
                {
                    x.Name = "Next Rank";
                    x.IsInline = true;
                    x.Value = $"Your next rank is {nextRole.Name} in another {timeSpan.ToPrettyFormat()}";
                });
            }

            var assignedRoles = await RankService.GetAssignedRolesAsync(Context.Guild.Id, user.Id).ConfigureAwait(false);
            if (assignedRoles.Any())
            {
                var sb = new StringBuilder();
                foreach (var role in assignedRoles)
                {
                    var name = Context.Guild.Roles.FirstOrDefault(x => x.Id == role.roleId).Name;
                    var expiry = role.expiry;
                    sb.AppendLine($"{name} expires in {(expiry - DateTimeOffset.UtcNow).Value.ToPrettyFormat()}");
                }
                embed.AddField(x =>
                {
                    x.Name = "Assigned Roles";
                    x.IsInline = true;
                    x.Value = sb.ToString();
                });
            }

            var guildColors = (await RankService.GetAvailableColors(user as SocketGuildUser, Context.Guild).ConfigureAwait(false)).ToList();
            if (guildColors.Any())
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
                string info = $"Last activity: {await RankService.GetUserLastActivity(user as SocketGuildUser, Context.Guild).ConfigureAwait(false)}\n" +
                              $"Joindate used: {await RankService.GetUserJoinDate(user as SocketGuildUser, Context.Guild).ConfigureAwait(false)}";
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

            embed.WithFooter(x => x.Text = "Hit the 📱 reaction if on mobile.");

            var msg = await ReplyAsync("", false, embed).ConfigureAwait(false);

            try
            {
                ReactionCallbackBuilder rcb = new ReactionCallbackBuilder();
                await rcb
                    .WithClient(Context.Client)
                    .WithPrecondition(x => x.Id == Context.User.Id)
                    .WithReactions("📱", "❌")
                    .AddCallback("📱", async func =>
                    {
                        await msg.ModifyAsync(message => message.Content =
                            string.Join(", ", Context.Guild.Roles.Where(x => guildColors.Any(y => y.Id == x.Id))
                                .OrderBy(x => x.Position)
                                .Select(z => z.Mention))).ConfigureAwait(false);
                    })
                    .AddCallback("❌", async func =>
                    {
                        await msg.DeleteAsync().ConfigureAwait(false);
                    })
                    .WithTimeout(120000)
                    .ExecuteAsync(msg)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Command("rank", RunMode = RunMode.Async)]
        [Alias("color", "colour")]
        [Summary("Select a rank available to you, as shown in the ranks command")]
        [RequireChannel(213359477162770433)]
        public Task RankCommand() => RanksCommand(Context.User);

        [Command("rank")]
        [Alias("color", "colour")]
        [Summary("Select a rank available to you, as shown in the ranks command")]
        [RequireChannel(213359477162770433)]
        public async Task RankCommand([Remainder]IRole role)
        {
            var availableColors = (await RankService.GetAvailableColors(Context.User as SocketGuildUser, Context.Guild).ConfigureAwait(false)).ToList();

            if (RankService.GetRanksForGuild(Context.Guild.Id).SelectMany(x => x.Colors).Select(x => x.Id).Union(availableColors.Select(x => x.Id)).All(y => y != role.Id))
            {
                await ReplyAsync("That role is not managed by this bot, try `?rank`").ConfigureAwait(false);
                return;
            }
            if (availableColors.Any(x => x.Id == role.Id))
            {
                var user = (IGuildUser) Context.User;
                //Check for any rank, since users can get demoted and then use this command before they get their grade back, might not be nessesary in the future
                var currentRank = RankService.GetRanksForGuild(Context.Guild.Id)
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
        public async Task AssignColorCommand(IUser user, IRole colorRole, [OverrideTypeReader(typeof(KiteTimeSpanReader))] TimeSpan timeToRemove = default(TimeSpan))
        {
            var timeSpan = timeToRemove != default(TimeSpan) ? (TimeSpan?)timeToRemove : null;
            var assignTask = await RankService.AssignColorToUser(Context.Guild.Id, user.Id, colorRole.Id, timeSpan).ConfigureAwait(false);
            await ReplyAsync(assignTask ? "OK" : "Couldn't remove rank").ConfigureAwait(false);
        }

        [Command("unassign"), Alias("deassign")]
        [Summary("assign an individual a color with an optional expiration")]
        [RequireOwnerOrUserPermission(GuildPermission.ManageGuild), RequireContext(ContextType.Guild)]
        public async Task UnassignColorCommand(IUser user, IRole role)
        {
            var result = await RankService.UnassignColorFromUserAsync(user, role).ConfigureAwait(false);
            await ReplyAsync(result ? "OK" : "Couldn't remove rank").ConfigureAwait(false);
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
                foreach (Json.Rank rank in list)
                {
                    stringBuilder.Append(Context.Guild.Roles.First(x => x.Id == rank.RoleId).Name)
                        .AppendLine($" => {rank.RequiredTimeSpan.ToPrettyFormat()}");
                    foreach (Json.Color color in rank.Colors)
                    {
                        stringBuilder.AppendLine($"\t{Context.Guild.Roles.First(x => x.Id == color.Id).Name}");
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
