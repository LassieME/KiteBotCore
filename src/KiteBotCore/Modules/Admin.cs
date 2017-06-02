using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;

namespace KiteBotCore.Modules
{
    public class Admin : CleansingModuleBase
    {
        private readonly CommandHandler _handler;
        private readonly IServiceProvider _services;

        public Admin(IServiceProvider services)
        {
            _handler = services.GetService<CommandHandler>();
            _services = services;
        }

        [Command("archive")]
        [Summary("archives a channel and uploads a JSON")]
        [RequireOwner]
        public async Task ArchiveCommand(string guildName, string channelName, int amount = 10000)
        {
            var channelToArchive = (await
                (await Context.Client.GetGuildsAsync().ConfigureAwait(false))
                .FirstOrDefault(x => x.Name == guildName)
                .GetTextChannelsAsync().ConfigureAwait(false))
                .FirstOrDefault(x => x.Name == channelName);

            if (channelToArchive != null)
            {
                var listOfMessages = new List<IMessage>(await channelToArchive.GetMessagesAsync(amount).Flatten().ConfigureAwait(false));
                List<Message> list = new List<Message>(listOfMessages.Capacity);
                foreach (var message in listOfMessages)
                    list.Add(new Message { Author = message.Author.Username, Content = message.Content, Timestamp = message.Timestamp });
                var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                var json = JsonConvert.SerializeObject(list, Formatting.Indented, jsonSettings);
                await Context.Channel.SendFileAsync(GenerateStreamFromString(json), $"{channelName}.json").ConfigureAwait(false);
            }
        }

        public class Message
        {
            public string Author;
            public string Content;
            public DateTimeOffset Timestamp;
        }

        private static MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.Unicode.GetBytes(value ?? ""));
        }

        [Command("save")]
        [Summary("saves markov chain messages")]
        [RequireOwner]
        public async Task SaveCommand()
        {
            var message = await ReplyAsync("OK").ConfigureAwait(false);
            var saveTask = KiteChat.MultiDeepMarkovChains.SaveAsync();
            await saveTask.ContinueWith(async (e) =>
            {
                if (e.IsCompleted) await message.ModifyAsync(x => x.Content += ", Saved.").ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [Command("saveexit")]
        [Alias("se")]
        [Summary("saves and exits")]
        [RequireOwner]
        public async Task SaveExitCommand()
        {
            var message = await ReplyAsync("OK").ConfigureAwait(false);
            var saveTask = KiteChat.MultiDeepMarkovChains?.SaveAsync();
            if (saveTask != null)
                await saveTask.ContinueWith(async (e) =>
                {
                    if (e.IsCompleted)
                        await message.ModifyAsync(x => x.Content = x.Content + ", Saved.").ConfigureAwait(false);
                }).ConfigureAwait(false);
            Environment.Exit(0);
        }

        [Command("update")]
        [Alias("up")]
        [Summary("Updates the livestream channel, and probably crashes if there is no chat")]
        [RequireOwner]
        public async Task UpdateCommand()
        {
            await KiteChat.StreamChecker.ForceUpdateChannel().ConfigureAwait(false);
            await ReplyAsync("updated?").ConfigureAwait(false);
        }

        [Command("delete")]
        [Alias("del")]
        [Summary("Deletes the last message the bot has written")]
        [RequireOwner]
        public async Task DeleteCommand()
        {
            if (KiteChat.BotMessages.Any()) await ((IUserMessage)KiteChat.BotMessages.Last()).DeleteAsync().ConfigureAwait(false);
        }

        [Command("restart")]
        [Alias("re")]
        [Summary("restarts the video and livestream checkers")]
        [RequireOwner]
        public async Task RestartCommand()
        {
            KiteChat.StreamChecker?.Restart();
            KiteChat.GbVideoChecker?.Restart();
            await ReplyAsync("It might have done something, who knows.").ConfigureAwait(false);
        }

        [Command("ignore")]
        [Summary("ignore a gb chat channelname")]
        [RequireOwner]
        public async Task IgnoreCommand([Remainder] string input)
        {
            KiteChat.StreamChecker.IgnoreChannel(input);
            await ReplyAsync("Added to ignore list.").ConfigureAwait(false);
        }

        [Command("listchannels")]
        [Summary("Lists names of GB chats")]
        [RequireOwner]
        public async Task ListChannelCommand()
        {

            await ReplyAsync("Current livestreams channels are:" + Environment.NewLine + await KiteChat.StreamChecker.ListChannelsAsync()
                .ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        [Command("say")]
        [Alias("echo")]
        [Summary("Echos the provided input")]
        [RequireOwner, RequireUserPermission(GuildPermission.Administrator)]
        public async Task SayCommand([Remainder] string input)
        {
            await ReplyAsync(input).ConfigureAwait(false);
        }

        [Command("embed")]
        [Summary("Echos the provided input")]
        [RequireOwner]
        public async Task EmbedCommand([Remainder] string input)
        {
            var embed = new EmbedBuilder
            {
                Title = input,
                Color = new Color(255, 0, 0),
                Description = input
            }.AddField(x =>
            {
                x.Name = input;
                x.Value = input;
            }).WithAuthor(x =>
            {
                x.Name = input;
            })
            .WithFooter(x =>
            {
                x.Text = input;
            });
            await ReplyAsync("", false, embed).ConfigureAwait(false);
        }

        [Command("setgame")]
        [Alias("playing")]
        [Summary("Sets a game in discord")]
        [RequireOwner]
        public async Task PlayingCommand([Remainder] string input)
        {
            var client = _services.GetService<DiscordSocketClient>();
            await client.SetGameAsync(input).ConfigureAwait(false);
        }

        [Command("setusername")]
        [Alias("username")]
        [Summary("Sets a new username for discord")]
        [RequireOwner]
        public async Task UsernameCommand([Remainder] string input)
        {
            var client = _services.GetService<DiscordSocketClient>();
            await client.CurrentUser.ModifyAsync(x => x.Username = input).ConfigureAwait(false);
        }

        [Command("setnickname")]
        [Alias("nickname")]
        [Summary("Sets a game in discord")]
        [RequireOwner, RequireContext(ContextType.Guild)]
        public async Task NicknameCommand([Remainder] string input)
        {
            await (await Context.Guild.GetCurrentUserAsync().ConfigureAwait(false)).ModifyAsync(x => x.Nickname = input).ConfigureAwait(false);
        }

        [Command("setavatar", RunMode = RunMode.Sync)]
        [Alias("avatar")]
        [Summary("Sets a new avatar image for this bot")]
        [RequireOwner]
        public async Task AvatarCommand([Remainder] string input)
        {
            var avatarStream = await new HttpClient().GetByteArrayAsync(input).ConfigureAwait(false);
            Stream stream = new MemoryStream(avatarStream);
            await Context.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(stream)).ConfigureAwait(false);
            await ReplyAsync("👌").ConfigureAwait(false);
        }

        [Command("help")]
        [Summary("Lists available commands")]
        public async Task Help(string optional = null)
        {
            string output = "";
            if (optional != null)
            {
                var command = _handler.Commands.Commands.FirstOrDefault(x => x.Aliases.Any(y => y == optional.ToLower()));
                if (command != null)
                {
                    output += $"Command: {string.Join(", ", command.Aliases)}: {Environment.NewLine}";
                    output += command.Summary;
                    await ReplyAsync(output + ".").ConfigureAwait(false);
                    return;
                }
                output += "Couldn't find a command with that name, givng you the commandlist instead:" +
                          Environment.NewLine;
            }
            foreach (CommandInfo cmdInfo in _handler.Commands.Commands.OrderBy(x => x.Aliases[0]))
            {
                try
                {
                    if ((await cmdInfo.CheckPreconditionsAsync(Context, _services).ConfigureAwait(false)).IsSuccess)
                    {
                        if (!string.IsNullOrWhiteSpace(output)) output += ",";
                        output += "`" + cmdInfo.Aliases[0] + "`";
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Exception in help command");
                    throw ex;
                }
            }
            output += "." + Environment.NewLine;
            await ReplyAsync("These are the commands you can use: " + Environment.NewLine + output + "Run help <command> for more information.").ConfigureAwait(false);
        }

        [Command("info")]
        [Summary("Contains info about the bot, such as owner, library, and runtime information")]
        public async Task Info()
        {
            string GetUptime() => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");

            string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.InvariantCulture);

            var application = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

            await ReplyAsync(
                $"{Format.Bold("Info")}\n" +
                $"- Author: {application.Owner.Username}#{application.Owner.DiscriminatorValue} (ID {application.Owner.Id})\n" +
                $"- Avatar by: UberX#6974\n" +
                $"- Source Code: <https://github.com/LassieME/KiteBotCore>\n" +
                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                $"- OS: {RuntimeInformation.OSDescription}\n" +
                $"- Uptime: {GetUptime()}\n\n" +

                $"{Format.Bold("Stats")}\n" +
                $"- Heap Size: {GetHeapSize()} MB\n" +
                $"- Guilds: {(Context.Client as DiscordSocketClient)?.Guilds.Count}\n" +
                $"- Channels: {(Context.Client as DiscordSocketClient)?.Guilds.Sum(g => g.Channels.Count)}" +
                $"- Users: {(Context.Client as DiscordSocketClient)?.Guilds.Sum(g => g.Users.Count)}"
            ).ConfigureAwait(false);
        }
    }
}
 