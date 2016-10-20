using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace KiteBotCore.Modules
{
    public class RaeCounterModule : ModuleBase
    {
        public static int RaeCount;

        static RaeCounterModule()
        {
            Program.Client.UserIsTyping += (s, e) =>
            {
                IsRaeTyping(s);
                return Task.CompletedTask;
            };

            Program.Client.MessageReceived += e =>
            {
                IsRaeTyping(e);
                return Task.CompletedTask;
            };
        }

        public static void IsRaeTyping(IMessage msg)
        {
            if (msg.Author.Id == 85876755797139456)
            {
                RaeCount += -1;
            }
        }

        public static void IsRaeTyping(IUser user)
        {
            if (user.Id == 85876755797139456)
            {
                RaeCount += 1;
            }
        }

        [Command("raecounter")]
        [Summary("Tells you how many times Rae has ghosttyped.")]
        public async Task RaeCounterCommand([Remainder] string animeTitle)
        {
            await ReplyAsync($"Rae has ghost-typed {RaeCount} times.");
        }
    }
}