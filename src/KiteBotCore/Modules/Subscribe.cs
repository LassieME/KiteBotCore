using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using KiteBotCore.Json.GiantBomb.Chats;
using KiteBotCore.Json.GiantBomb.GbUpcoming;
using Newtonsoft.Json;
using Serilog;

namespace KiteBotCore.Modules
{
    public class Subscribe : ModuleBase //TODO: Decouple the module and the static service
    {
        private static string SubscriberFilePath => Directory.GetCurrentDirectory() + "/Content/subscriber.json";

        public static List<ulong> SubscriberList = File.Exists(SubscriberFilePath) ? JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText(SubscriberFilePath))
                : new List<ulong>();

        private Stopwatch _stopwatch;
        protected override void BeforeExecute(CommandInfo command)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute(CommandInfo command)
        {
            _stopwatch.Stop();
            Log.Debug($"Subscribe Command: {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }

        [Command("subscribe"), Alias("sub")]
        [Summary("Subscribes to livestream DMs")]
        public async Task SubscribeCommands()
        {
            if (SubscriberList.Contains(Context.User.Id) )
            {
                await ReplyAsync("You're already subscribed, to unsubscribe use \"~unsubscribe\"").ConfigureAwait(false);
            }
            else
            {
                AddToList(Context.User.Id);
                await ReplyAsync("You are now subscribed, to unsubscribe use \"~unsubscribe\". You have to stay in the server to continue to get messages.").ConfigureAwait(false);
            }
        }
        [Command("unsubscribe"), Alias("unsub")]
        [Summary("Unsubscribes to livestream DMs")]
        public async Task UnsubscribeCommands()
        {
            if (SubscriberList.Contains(Context.User.Id))
            {
                RemoveFromList(Context.User.Id);

                await ReplyAsync("You are now unsubscribed, thanks for trying it out.").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("You are already unsubscribed.").ConfigureAwait(false);
            }
        }

        internal static async Task PostLivestream(Result stream, DiscordSocketClient client)
        {
            var title = stream.Title;
            var deck = stream.Deck;
            var image = stream.Image.ScreenUrl;
            
            foreach (ulong user in SubscriberList.ToArray())
            {
                try
                {
                    var channel = await client.GetUser(user).GetOrCreateDMChannelAsync().ConfigureAwait(false);
                    await channel.SendMessageAsync(title + ": " + deck +
                                                   " is LIVE at <http://www.giantbomb.com/chat/> NOW, check it out!" +
                                                   Environment.NewLine + (image ?? ""))
                        .ConfigureAwait(false);
                }
                catch (Discord.Net.HttpException httpException)
                {
                    if (httpException.DiscordCode == 50007)
                    {
                        Log.Information(httpException, "couldn't send {user} a DM, removing", user);
                        RemoveFromList(user);
                    }
                    else
                    {
                        Log.Warning(httpException, "A unhandled error happened in Subscribe.PostLivestream");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "A unhandled error happened in Subscribe.PostLivestream");
                }
            }
        }

        internal static async Task PostLivestream(IMessage message, DiscordSocketClient client)
        {
            foreach (ulong user in SubscriberList.ToArray())
            {
                try
                {
                    var channel = await client.GetUser(user).GetOrCreateDMChannelAsync().ConfigureAwait(false);

                    await channel.SendMessageAsync(message.Content, embed: message.Embeds.FirstOrDefault()?.ToEmbedBuilder().Build());
                }
                catch (Discord.Net.HttpException httpException)
                {
                    if (httpException.DiscordCode == 50007)
                    {
                        Log.Information(httpException, "couldn't send {user} a DM, removing", user);
                        RemoveFromList(user);
                    }
                    else
                    {
                        Log.Warning(httpException, "A unhandled error happened in Subscribe.PostLivestream");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "A unhandled error happened in Subscribe.PostLivestream");
                }
            }
        }

        private static void AddToList(ulong user)
        {
            SubscriberList.Add(user);
            Save();
        }

        private static void RemoveFromList(ulong user)
        {
            SubscriberList.Remove(user);
            Save();
        }

        private static void Save()
        {
            File.WriteAllText(SubscriberFilePath, JsonConvert.SerializeObject(SubscriberList));
        }
    }
}
