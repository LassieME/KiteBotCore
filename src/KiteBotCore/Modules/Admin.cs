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
        [Command("saveexit")]
        [Alias("se")]
        [Summary("saves and exits.")]
        [RequireOwner]
        public async Task SaveExitCommand()
        {
            await ReplyAsync("OK");
            Environment.Exit(1);
        }

        [Command("update")]
        [Alias("up")]
        [Summary("Updates the livestream channel, and probably crashes if there is no chat.")]
        [RequireOwner]
        public async Task UpdateCommand()
        {
            await KiteChat.StreamChecker.ForceUpdateChannel();
            await ReplyAsync("updated?");
        }

        [Command("delete")]
        [Alias("del")]
        [Summary("Deletes the last message the bot has written.")]
        [RequireOwner]
        public async Task DeleteCommand()
        {
            if (KiteChat.BotMessages.Any()) await ((IUserMessage) KiteChat.BotMessages.Last()).DeleteAsync();
        }

        [Command("restart")]
        [Alias("re")]
        [Summary("restarts the video and livestream checkers.")]
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
        public async Task ListChannelCommand([Remainder] string input)
        {
            await ReplyAsync(await KiteChat.StreamChecker.ListChannels());
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
        public async Task Help()
        {
            string output = "";
            
            await ReplyAsync(output +".");
        }

        [Command("info")]
        public async Task Info()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            await ReplyAsync(
                $"{Format.Bold("Info")}\n" +
                $"- Author: {application.Owner.Username} (ID {application.Owner.Id})\n" +
                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
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