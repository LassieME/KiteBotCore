using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using KiteBotCore.Json;
using KiteBotCore.Modules;
using Newtonsoft.Json;

namespace KiteBotCore
{
    public class Program
    {
        public static DiscordSocketClient Client;
        public static CommandService CommandService = new CommandService();
        public static BotSettings Settings;
        public static string ContentDirectory = Directory.GetCurrentDirectory();

        private static KiteChat _kiteChat;
        private static string SettingsPath => ContentDirectory + "/Content/settings.json";
        private static CommandHandler _handler;

        // ReSharper disable once UnusedMember.Local
        private static void Main(string[] args) => AsyncMain(args).GetAwaiter().GetResult();

        public static async Task AsyncMain(string[] args)
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 0
            });

            Settings = File.Exists(SettingsPath) ?
                JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(SettingsPath))
                : new BotSettings
                {
                    DiscordEmail = "email",
                    DiscordPassword = "password",
                    DiscordToken = "Token",
                    GiantBombApiKey = "GBAPIKey",
                    OwnerId = 0,
                    MarkovChainStart = false,
                    MarkovChainDepth = 2,
                    GiantBombLiveStreamRefreshRate = 60000,
                    GiantBombVideoRefreshRate = 60000
                };

            _kiteChat = new KiteChat(Settings.MarkovChainStart,
                Settings.GiantBombApiKey,
                Settings.GiantBombLiveStreamRefreshRate,
                Settings.GiantBombVideoRefreshRate,
                Settings.MarkovChainDepth);

            Client.MessageReceived += async msg =>
            {
                try
                {
                    await _kiteChat.AsyncParseChat(msg, Client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex);
                    Environment.Exit(-1);
                }
            };

            Client.GuildAvailable += async server =>
            {
                if (Client.Guilds.Any())
                {
                    var markovChainDone = await _kiteChat.InitializeMarkovChain();
                    Console.WriteLine(markovChainDone);
                }
            };

            Client.JoinedGuild += server =>
            {
                Console.WriteLine("Connected to " + server.Name);
                return Task.CompletedTask;
            };

            Client.GuildMemberUpdated += async (before, after) =>
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
                            await channel.SendMessageAsync($"{before.Nickname} changed his nickname to {after.Nickname}.");
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
            };
            Console.WriteLine("LoginAsync");
            await Client.LoginAsync(TokenType.Bot, Settings.DiscordToken);
            await Client.ConnectAsync();

            var map = new DependencyMap();
            map.Add(Client);
            map.Add(Settings);
            map.Add(_kiteChat);

            _handler = new CommandHandler();
            await _handler.Install(map);

            await Task.Delay(-1);
        }
    }
}
