using Discord;
using Discord.Commands;
using Discord.WebSocket;
using KiteBotCore.Json;
using KiteBotCore.Modules;
using KiteBotCore.Modules.Giantbomb;
using KiteBotCore.Modules.Reminder;
using KiteBotCore.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using KiteBotCore.Modules.RankModule;

namespace KiteBotCore
{
    public partial class Program
    {
        public static DiscordSocketClient Client;

        private static KiteChat _kiteChat;
        private static BotSettings _settings;
        private static CommandHandler _handler;
        private static CommandService _commandService;
        private static DiscordContextFactory _dbFactory;
        private static RankConfigs _rankConfigs;
        private static bool _silentStartup;
        private static bool _isFirstTime = true;
        private static string SettingsPath => Directory.GetCurrentDirectory() + "/Content/settings.json";
        private static string RankConfigPath => Directory.GetCurrentDirectory() + "/Content/rankconfig.json";

        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Async(a => a.RollingFile("rollinglog.log", fileSizeLimitBytes: 50000000).MinimumLevel.Information())
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
                HandlerTimeout = 2000
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

            _rankConfigs = File.Exists(RankConfigPath)
                ? JsonConvert.DeserializeObject<RankConfigs>(File.ReadAllText(RankConfigPath))
                : new RankConfigs
                {
                    GuildConfigs = new Dictionary<ulong, GuildRanks>()
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
            
            Client.MessageReceived += (msg) =>
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

            Client.JoinedGuild += (server) =>
            {
                Log.Information("Connected to {Name}", server.Name);
                return Task.CompletedTask;
            };

            Client.UserUnbanned += (user, guild) =>
            {
                Console.WriteLine($"{user.Username}#{user.Discriminator}");
                Console.WriteLine(user);
                return Task.CompletedTask;
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

            Client.Ready += OnReady;
            
            Client.UserUpdated += async (before, after) => await CheckUsername(before, after).ConfigureAwait(false);
            Client.GuildMemberUpdated += async (before, after) => await CheckNickname(before, after).ConfigureAwait(false);

            await Task.Delay(-1).ConfigureAwait(false);
        }

        private static async Task OnReady()
        {
            try
            {
                if (_isFirstTime)
                {
                    using (var db = _dbFactory.Create())
                    {
                        var serviceProvider = db.GetInfrastructure();
                        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                        loggerFactory.AddProvider(new MyLoggerProvider());
                    }

                    var services = new ServiceCollection();
                    _handler = new CommandHandler();
                    services.AddSingleton(Client);
                    services.AddSingleton(_settings);
                    services.AddSingleton(new RankService(_rankConfigs, configs => File.WriteAllText(RankConfigPath,JsonConvert.SerializeObject(_rankConfigs,Formatting.Indented)), _dbFactory, Client));
                    services.AddSingleton(_kiteChat);
                    services.AddSingleton(_handler);
                    services.AddEntityFramework()
                        .AddEntityFrameworkNpgsql()
                        .AddDbContext<KiteBotDbContext>(options => options.UseNpgsql(_settings.DatabaseConnectionString));
                    services.AddSingleton(new VideoService(_settings.GiantBombApiKey));
                    services.AddSingleton(new SearchHelper(_settings.AnilistId, _settings.AnilistSecret));
                    services.AddSingleton(new ReminderService(Client));
                    services.AddSingleton(new FollowUpService());
                    services.AddSingleton(new Random());
                    services.AddSingleton(new CryptoRandom());

                    await _handler.InstallAsync(_commandService, services.BuildServiceProvider()).ConfigureAwait(false);

                    var _ = TryRun(async () =>
                    {
                        var sw = new Stopwatch();
                        sw.Start();
                        await _kiteChat.InitializeMarkovChainAsync().ConfigureAwait(false);
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
                    Log.Information("{Source} {Message} {Exception}", msg.Source, msg.Message, msg.Exception?.ToString());
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

        public static async Task CheckNickname(SocketGuildUser before, SocketGuildUser after)
        {
            try
            {
                if (before?.Guild.Id == 85814946004238336)
                {
                    var channel = (ITextChannel) Client.GetChannel(85842104034541568);
                    if (channel != null && before.Nickname != after.Nickname)
                    {
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + "\r\n" + ex.Message);
            }
        }

        public static async Task CheckUsername(SocketUser before, SocketUser after)
        {
            try
            {

                if (before.Username != after.Username)
                {
                    var channel = (ITextChannel) Client.GetChannel(85842104034541568);
                    if (channel != null)
                    {
                        await channel.SendMessageAsync($"{before.Username} changed his name to {after.Username}.")
                            .ConfigureAwait(false);
                        WhoIsService.AddWhoIs(before, after);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + "\r\n" + ex.Message);
            }
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
                    await function().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, ex.Message);
                }
            });
        }
    }
}