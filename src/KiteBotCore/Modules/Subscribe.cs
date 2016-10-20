using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using KiteBotCore.Json.GiantBomb.Chats;
using Newtonsoft.Json;

namespace KiteBotCore.Modules
{
    public class Subscribe : ModuleBase
    {
        private static string SubscriberFilePath => Directory.GetCurrentDirectory() + "/Content/subscriber.json";

        public static List<ulong> SubscriberList = File.Exists(SubscriberFilePath) ? JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText(SubscriberFilePath))
                : new List<ulong>();

        public static DiscordSocketClient Client = Program.Client;

        [Command("subscribe")]
        [Summary("Subscribes to livestream DMs")]
        public async Task SubscribeCommands()
        {
            if (SubscriberList.Contains(Context.User.Id) )
            {
                await ReplyAsync("You're already subscribed, to unsubscribe use \"~unsubscribe\"");
            }
            else
            {
                AddToList(Context.User.Id);
                await ReplyAsync("You are now subscribed, to unsubscribe use \"~unsubscribe\". You have to stay in the GB server to continue to get messages.");
            }
        }
        [Command("unsubscribe")]
        [Summary("Unsubscribes to livestream DMs")]
        public async Task UnsubscribeCommands()
        {
            if (SubscriberList.Contains(Context.User.Id))
            {
                RemoveFromList(Context.User.Id);
                await
                    ReplyAsync("You are now unsubscribed, thanks for trying it out.");
            }
            else
            {
                await
                    ReplyAsync("You are already unsubscribed.");
            }
        }

        internal static async Task PostLivestream(Result stream)
        {
            var title = stream.Title;
            var deck = stream.Deck;
            var image = stream.Image.ScreenUrl;
            
            foreach (ulong user in SubscriberList)
            {
                try
                {
                    var channel = await Client.GetUser(user).CreateDMChannelAsync();
                    await channel.SendMessageAsync(title + ": " + deck + " is LIVE at <http://www.giantbomb.com/chat/> NOW, check it out!" +
                                Environment.NewLine + image, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + Environment.NewLine + ex.Message);
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
