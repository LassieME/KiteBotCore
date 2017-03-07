using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using KiteBotCore.Json;
using KiteBotCore.Modules;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Serilog;

namespace KiteBotCore
{
    public class Program
    {
        public static DiscordSocketClient Client;

        private static KiteChat _kiteChat;
        private static BotSettings _settings;
        private static CommandHandler _handler;
        private static DiscordContextFactory _dbFactory;
        private static string SettingsPath => Directory.GetCurrentDirectory() + "/Content/settings.json";
        private static bool _silentStartup;

        // ReSharper disable once UnusedMember.Local
        private static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        public static async Task MainAsync(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .MinimumLevel.Debug()
                .CreateLogger();

            if (args.Length != 0 && (args[0].Contains("--silent") || args[0].Contains("-s")))
            {
                _silentStartup = true;
            }
            else
            {
                Log.Warning("Are you sure you shouldn't be using the --silent argument?");
            }

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 0,
                AudioMode = AudioMode.Outgoing
            });

            _settings = File.Exists(SettingsPath) ?
                JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(SettingsPath))
                : new BotSettings
                {
                    CommandPrefix = '!',
                    DiscordEmail = "email",
                    DiscordPassword = "password",
                    DiscordToken = "Token",
                    GiantBombApiKey = "GbAPIKey",
                    YoutubeApiKey = "",
                    DatabaseConnectionString = "",
                    OwnerId = 0,
                    MarkovChainStart = false,
                    MarkovChainDepth = 2,
                    GiantBombLiveStreamRefreshRate = 60000,
                    GiantBombVideoRefreshRate = 60000
                };
            _dbFactory = new DiscordContextFactory();

            _kiteChat = new KiteChat(Client, _dbFactory,
                _settings.MarkovChainStart,
                _settings.GiantBombApiKey,
                _settings.YoutubeApiKey,
                _settings.GiantBombLiveStreamRefreshRate,
                _silentStartup,
                _settings.GiantBombVideoRefreshRate,
                _settings.MarkovChainDepth);

            Client.Log += LogDiscordMessage;


            Client.MessageReceived += msg =>
            {
                Log.Verbose("MESSAGE {Channel}{tab}{User}: {Content}", msg.Channel.Name, "\t", msg.Author.Username, msg.ToString());
                return Task.CompletedTask;
            };

            Client.GuildAvailable += async server =>
            {
                var sw = new Stopwatch();
                sw.Start();
                await _dbFactory.SyncGuild(server);
                sw.Stop();
                Log.Information("{sw} ms",sw.ElapsedMilliseconds);
                await _kiteChat.InitializeMarkovChainAsync().ConfigureAwait(false);

                Log.Information("Ready: {Done}", server.Name);
            };

            Client.JoinedGuild += server =>
            {
                Log.Information("Connected to {Name}", server.Name);
                return Task.CompletedTask;
            };

            Client.GuildMemberUpdated += async (before, after) =>
            {
                if (before.Guild.Id == 85814946004238336)
                {
                    var channel = (ITextChannel)Client.GetChannel(85842104034541568);
                    if (!before.Username.Equals(after.Username))
                    {
                        await channel.SendMessageAsync($"{before.Username} changed his name to {after.Username}.");
                        WhoIsService.AddWhoIs(before, after);
                    }
                    try
                    {
                        if (before.Nickname != after.Nickname)
                        {
                            if (before.Nickname != null && after.Nickname != null)
                            {
                                await channel.SendMessageAsync(
                                    $"{before.Nickname} changed his nickname to {after.Nickname}.");
                                WhoIsService.AddWhoIs(before, after.Nickname);
                            }
                            else if (before.Nickname == null && after.Nickname != null)
                            {
                                await channel.SendMessageAsync($"{before.Username} set his nickname to {after.Nickname}.");
                                WhoIsService.AddWhoIs(before, after.Nickname);
                            }
                            else
                            {
                                await channel.SendMessageAsync($"{before.Username} reset his nickname.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex + "\r\n" + ex.Message);
                    }
                }
            };

            Console.WriteLine("LoginAsync");
            await Client.LoginAsync(TokenType.Bot, _settings.DiscordToken);
            Console.WriteLine("ConnectAsync");
            await Client.StartAsync();

            var map = new DependencyMap();
            _handler = new CommandHandler();
            map.Add(Client);
            map.Add(_settings);
            map.Add(_kiteChat);
            map.Add(_handler);
            map.Add(_dbFactory);
            map.Add(new AnimeManga.SearchHelper(_settings.AnilistId, _settings.AnilistSecret));

            await _handler.InstallAsync(map);
            await Task.Delay(-1);
        }

        private static Task LogDiscordMessage(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    Log.Fatal("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString());
                    break;
                case LogSeverity.Error:
                    Log.Error("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString());
                    break;
                case LogSeverity.Warning:
                    Log.Warning("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString());
                    break;
                case LogSeverity.Info:
                    Log.Information("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString());
                    break;
                case LogSeverity.Verbose: //Verbose and Debug are switched between Serilog and Discord.Net
                    Log.Debug("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString());
                    break;
                case LogSeverity.Debug:
                    Log.Verbose("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString());
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
