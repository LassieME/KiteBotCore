using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using KiteBotCore.Json;
using KiteBotCore.Modules;
using KiteBotCore.Utils;
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
        private static CommandService _commandService;
        private static DiscordContextFactory _dbFactory;
        private static bool _silentStartup;
        private static string SettingsPath => Directory.GetCurrentDirectory() + "/Content/settings.json";
        private static bool _isFirstTime = true;

        // ReSharper disable once UnusedMember.Local
        private static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        public static async Task MainAsync(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Async(a => a.File("log.txt").MinimumLevel.Information())
                .WriteTo.LiterateConsole()
                .MinimumLevel.Debug()
                .CreateLogger();

            if (args.Length != 0 && (args[0].Contains("--silent") || args[0].Contains("-s")))
                _silentStartup = true;
            else
                Log.Information("Are you sure you shouldn't be using the --silent argument?");

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 0,
                AlwaysDownloadUsers = true,
                HandlerTimeout = null
            });

            _settings = File.Exists(SettingsPath)
                ? JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(SettingsPath))
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

            _commandService = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Sync,
                LogLevel = LogSeverity.Verbose,
                SeparatorChar = ' ',
                ThrowOnError = true //Throws exceptions up to the commandhandler in sync commands
            });

            Client.Log += LogDiscordMessage;
            _commandService.Log += LogDiscordMessage;


            Client.MessageReceived += msg =>
            {
                Log.Verbose("MESSAGE {Channel}{tab}{User}: {Content}", msg.Channel.Name, "\t", msg.Author.Username,
                    msg.ToString());
                return Task.CompletedTask;
            };

            Client.GuildMembersDownloaded += async (guild) =>
            {
                var sw = new Stopwatch();
                sw.Start();
                await _dbFactory.SyncGuild(guild).ConfigureAwait(false);
                sw.Stop();
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
                    var channel = (ITextChannel) Client.GetChannel(85842104034541568);
                    if (before.Username != after.Username)
                    {
                        await channel.SendMessageAsync($"{before.Username} changed his name to {after.Username}.")
                            .ConfigureAwait(false);
                        WhoIsService.AddWhoIs(before, after);
                    }
                    try
                    {
                        if (before.Nickname != after.Nickname)
                            if (before.Nickname != null && after.Nickname != null)
                            {
                                await channel.SendMessageAsync(
                                        $"{before.Nickname} changed his nickname to {after.Nickname}.")
                                    .ConfigureAwait(false);
                                WhoIsService.AddWhoIs(before, after.Nickname);
                            }
                            else if (before.Nickname == null && after.Nickname != null)
                            {
                                await channel
                                    .SendMessageAsync($"{before.Username} set his nickname to {after.Nickname}.")
                                    .ConfigureAwait(false);
                                WhoIsService.AddWhoIs(before, after.Nickname);
                            }
                            else
                            {
                                await channel.SendMessageAsync($"{before.Username} reset his nickname.")
                                    .ConfigureAwait(false);
                            }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex + "\r\n" + ex.Message);
                    }
                }
            };
            int connections = 0;
            Client.Connected += () =>
            {
                Console.WriteLine($"Connected for the {connections++} time.");
                return Task.CompletedTask;
            };

            int disconnections = 0;
            Client.Disconnected += (exception) =>
            {
                Console.WriteLine($"Disconnected for the {disconnections++} time.");
                return Task.CompletedTask;
            };

            Console.WriteLine("LoginAsync");
            await Client.LoginAsync(TokenType.Bot, _settings.DiscordToken).ConfigureAwait(false);
            Console.WriteLine("ConnectAsync");
            await Client.StartAsync().ConfigureAwait(false);

            Client.Ready += async () =>
            {
                try
                {
                    if (_isFirstTime)
                    {
                        var map = new DependencyMap();
                        _handler = new CommandHandler();
                        map.Add(Client);
                        map.Add(_settings);
                        map.Add(_kiteChat);
                        map.Add(_handler);
                        map.AddFactory(() => _dbFactory.Create(new DbContextFactoryOptions()));
                        map.Add(new AnimeManga.SearchHelper(_settings.AnilistId, _settings.AnilistSecret));
                        map.Add(new Random());
                        map.Add(new CryptoRandom());

                        await _handler.InstallAsync(_commandService, map).ConfigureAwait(false);

                        var initTask = TryRun(async () =>
                        {
                            var sw = new Stopwatch();
                            sw.Start();
                            await _kiteChat.InitializeMarkovChainAsync();
                            sw.Stop();
                            Log.Information("Initialize Markov Chain: Done,({sw} ms)", sw.ElapsedMilliseconds);
                        });
                        _isFirstTime = false;
                    }
                    Log.Information("Ready: Done");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception in Ready Event");
                }
            };

            await Task.Delay(-1).ConfigureAwait(false);
        }

        private static Task LogDiscordMessage(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    Log.Fatal("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString() ?? "");
                    break;
                case LogSeverity.Error:
                    Log.Error("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString() ?? "");
                    break;
                case LogSeverity.Warning:
                    Log.Warning("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString() ?? "");
                    break;
                case LogSeverity.Info:
                    Log.Information("{Source} {Message} {Exception}", msg.Source, msg.Message,
                        msg.Exception?.ToString());
                    break;
                case LogSeverity.Verbose: //Verbose and Debug are switched between Serilog and Discord.Net
                    Log.Debug("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString() ?? "");
                    break;
                case LogSeverity.Debug:
                    Log.Verbose("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString() ?? "");
                    break;
            }
            return Task.CompletedTask;
        }

        public static Task TryRun(Action action)
        {
            return Task.Run(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, ex.Message);
                }
            });
        }

        public static Task TryRun(Func<Task> function)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await function();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, ex.Message);
                }
            });
        }
    }
}