using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;

namespace KiteBotCore.Modules
{
    public class Admin : ModuleBase
    {
        private readonly CommandService _handler;
        private readonly IDependencyMap _map;

        public Admin(IDependencyMap map)
        {
            _handler = map.Get<CommandService>();
            _map = map;
        }

        [Command("save")]
        [Summary("saves markovchainmessages")]
        [RequireOwner]
        public async Task SaveCommand()
        {
            var message = await ReplyAsync("OK");
            var saveTask = KiteChat.MultiDeepMarkovChains.Save();
            await saveTask.ContinueWith(async (e) =>
            {
                if (e.IsCompleted) await message.ModifyAsync(x => x.Content += ", Saved.");
            });
        }

        [Command("saveexit")]
        [Alias("se")]
        [Summary("saves and exits")]
        [RequireOwner]
        public async Task SaveExitCommand()
        {
            var message = await ReplyAsync("OK");
            var saveTask = KiteChat.MultiDeepMarkovChains.Save();
            await saveTask.ContinueWith(async (e) =>
            {
                if (e.IsCompleted) await message.ModifyAsync(x => x.Content += ", Saved.");
            });            
            Environment.Exit(0);
        }

        [Command("update")]
        [Alias("up")]
        [Summary("Updates the livestream channel, and probably crashes if there is no chat")]
        [RequireOwner]
        public async Task UpdateCommand()
        {
            await KiteChat.StreamChecker.ForceUpdateChannel();
            await ReplyAsync("updated?");
        }

        [Command("delete")]
        [Alias("del")]
        [Summary("Deletes the last message the bot has written")]
        [RequireOwner]
        public async Task DeleteCommand()
        {
            if (KiteChat.BotMessages.Any()) await ((IUserMessage) KiteChat.BotMessages.Last()).DeleteAsync();
        }

        [Command("restart")]
        [Alias("re")]
        [Summary("restarts the video and livestream checkers")]
        [RequireOwner]
        public async Task RestartCommand()
        {
            KiteChat.StreamChecker?.Restart();
            KiteChat.GbVideoChecker?.Restart();
            await ReplyAsync("It might have done something, who knows.");
        }

        [Command("ignore")]
        [Summary("ignore a gb chat channelname")]
        [RequireOwner]
        public async Task IgnoreCommand([Remainder] string input)
        {
            KiteChat.StreamChecker.IgnoreChannel(input);
            await ReplyAsync("Added to ignore list.");
        }

        [Command("listchannels")]
        [Summary("Lists names of GB chats")]
        [RequireOwner]
        public async Task ListChannelCommand()
        {

            await ReplyAsync("Current livestreams channels are:" + Environment.NewLine + (await KiteChat.StreamChecker.ListChannels()));
        }

        [Command("say")]
        [Alias("echo")]
        [Summary("Echos the provided input")]
        [RequireOwner]
        public async Task Say([Remainder] string input)
        {
            await ReplyAsync(input);
        }

        [Command("help")]
        [Summary("Lists availible commands")]
        public async Task Help(string optional = null)
        {
            string output = "";
            if (optional != null)
            {
                var command = _handler.Commands.FirstOrDefault(x => x.Aliases.Any(y => y.Equals(optional.ToLower())));
                if (command != null)
                {
                    output += $"Command: {string.Join(", ", command.Aliases)}: {Environment.NewLine}";
                    output += command.Summary;
                    await ReplyAsync(output + ".");
                    return;
                }
                output += "Couldn't find a command with that name, givng you the commandlist instead:" +
                          Environment.NewLine;
            }
            output += "These are the commands you can use: " + Environment.NewLine;
            foreach (CommandInfo cmdInfo in _handler.Commands)
            {
                if ((await cmdInfo.CheckPreconditions(Context, _map)).IsSuccess)
                {
                    if (!string.IsNullOrWhiteSpace(output)) output += ",";
                    output += "`" + cmdInfo.Aliases[0] + "`";
                }
            }
            output += "." + Environment.NewLine;
            await ReplyAsync(output + "Run help <command> for more information.");
        }

        [Command("info")]
        [Summary("Contains info about the bot, such as owner, library, and runtime information")]
        public async Task Info()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            await ReplyAsync(
                $"{Format.Bold("Info")}\n" +
                $"- Author: {application.Owner.Username}#{application.Owner.DiscriminatorValue} (ID {application.Owner.Id})\n" +
                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                $"- OS: {RuntimeInformation.OSDescription}\n" +
                $"- Uptime: {GetUptime()}\n\n" +

                $"{Format.Bold("Stats")}\n" +
                $"- Heap Size: {GetHeapSize()} MB\n" +
                $"- Guilds: {(Context.Client as DiscordSocketClient)?.Guilds.Count}\n" +
                $"- Channels: {(Context.Client as DiscordSocketClient)?.Guilds.Sum(g => g.Channels.Count)}" +
                $"- Users: {(Context.Client as DiscordSocketClient)?.Guilds.Sum(g => g.Users.Count)}"
            );
        }

        private static string GetUptime() => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
        private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.InvariantCulture);
    }
}