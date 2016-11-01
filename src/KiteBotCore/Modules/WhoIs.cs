using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;

namespace KiteBotCore.Modules
{
    public class WhoIsModule : ModuleBase
    {
        [Command("whois")]
        [Summary("lists nicknames and usernames the mentioned person has had before")]
        [RequirePermission(GuildPermission.Administrator)]
        [RequireServer(Server.KiteCo)]
        public async Task GetWhoIs()
        {
            var userMentioned = Context.Message.MentionedUserIds.FirstOrDefault();
            if (userMentioned != 0)
            {
                await
                    ReplyAsync(
                        $"Former names for {await Context.Client.GetUserAsync(userMentioned)} are: {WhoIsService.EnumWhoIs(userMentioned)}."
                            .Replace(
                                ",.", "."));
            }
        }
    }

    public static class WhoIsService
    {
        internal static string ChatDirectory = Directory.GetCurrentDirectory();
        internal static string WhoIsLocation = ChatDirectory + "/Content/Whois.json";
        internal static Dictionary<ulong, WhoIsPerson> WhoIsDictionary = File.Exists(WhoIsLocation)
                ? JsonConvert.DeserializeObject<Dictionary<ulong, WhoIsPerson>>(File.ReadAllText(WhoIsLocation))
                : new Dictionary<ulong, WhoIsPerson>();

        public static void AddWhoIs(IUser before, IUser after)
        {
            if (WhoIsDictionary.ContainsKey(after.Id))
            {
                WhoIsDictionary[after.Id].OldNames.Add(after.Username);
            }
            else
            {
                string[] names = { before.Username, after.Username };
                WhoIsDictionary.Add(after.Id, new WhoIsPerson
                {
                    UserId = after.Id,
                    OldNames = new List<string>(names)
                });
            }
            File.WriteAllText(WhoIsLocation, JsonConvert.SerializeObject(WhoIsDictionary));
        }

        public static void AddWhoIs(IUser user, string nicknameAfter)
        {
            if (WhoIsDictionary.ContainsKey(user.Id))
            {
                WhoIsDictionary[user.Id].OldNames.Add(nicknameAfter);
            }
            else
            {
                string[] names = { user.Username, nicknameAfter };
                WhoIsDictionary.Add(user.Id, new WhoIsPerson
                {
                    UserId = user.Id,
                    OldNames = new List<string>(names)
                });
            }
            File.WriteAllText(WhoIsLocation, JsonConvert.SerializeObject(WhoIsDictionary));
        }

        public static string EnumWhoIs(ulong id)
        {
            WhoIsPerson person;
            if (WhoIsDictionary.TryGetValue(id, out person))
            {
                var output = "";
                var list = person.OldNames;
                foreach (var name in list)
                {
                    output += $"{name},";
                }
                return output;
            }
            return "No former names found,";
        }
    }
    internal class WhoIsPerson
    {
        public ulong UserId { get; set; }
        public List<string> OldNames { get; set; }
    }
}
