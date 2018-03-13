using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;

namespace KiteBotCore.Modules.Music
{
    public class SoundEffect : CleansingModuleBase
    {
        public DiscordSocketClient Client { get; set; }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("What is love")]
        [RequireBotOwner, RequireContext(ContextType.Guild)]
        public async Task MusicTestCommand(IChannel channel)
        {
            //var channel = (Context.User as SocketGuildUser)?.VoiceChannel;
            Debug.Assert(channel != null);
            const string path = @"D:\Users\sindr\Downloads\MarkovChristmas.mp3";
            try
            {
                using (var audioClient = await (channel as IVoiceChannel)?.ConnectAsync())
                using (var stream = audioClient.CreatePCMStream(AudioApplication.Music, bitrate: (channel as IVoiceChannel)?.Bitrate, bufferMillis: 2880))
                {
                    using (var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-i \"{path}\" -f s16le -ar 48000 -ac 2 pipe:1",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }))
                    {
                        if (process != null)
                        {
                            Task flush = process.StandardError.BaseStream.CopyToAsync(Stream.Null);
                            await process.StandardOutput.BaseStream.CopyToAsync(stream).ConfigureAwait(false);
                            await flush.ConfigureAwait(false);
                            process.WaitForExit();
                        }
                    }

                    await ReplyAsync("👌").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + ex.Message);
            }
        }
    }
}
