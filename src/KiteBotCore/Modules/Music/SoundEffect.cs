using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;

namespace KiteBotCore.Modules.Music
{
    public class SoundEffect : CleansingModuleBase
    {
        private readonly DiscordSocketClient _client;

        public SoundEffect(DiscordSocketClient client)
        {
            _client = client;
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("What is love")]
        [RequireOwner, RequireContext(ContextType.Guild)]
        public async Task MusicTestCommand()
        {
            var channel = (Context.User as SocketGuildUser)?.VoiceChannel;
            Debug.Assert(channel != null);
            const string path = @"D:\Users\sindr\Downloads\MarkovChristmas.mp3";
            try
            {
                using (var audioClient = await channel.ConnectAsync())
                using (var stream = audioClient.CreatePCMStream(AudioApplication.Music, 2880, bitrate: channel.Bitrate))
                {
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-i \"{path}\" -f s16le -ar 48000 -ac 2 pipe:1",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    });
                    Task flush = process.StandardError.BaseStream.CopyToAsync(Stream.Null);
                    await process.StandardOutput.BaseStream.CopyToAsync(stream);
                    await flush;
                    process.WaitForExit();
                    await ReplyAsync("👌");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + ex.Message);
            }
        }
    }
}
