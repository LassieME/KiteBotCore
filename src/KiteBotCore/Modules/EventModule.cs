using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace KiteBotCore.Modules
{
    [RequireBotOwner]
    public class EventModule : ModuleBase
    {
        [Command("event add")]
        public async Task EventAddCommand(string title, DateTimeOffset dateTime, [Remainder]string description)
        {

        }

        [Command("event remove")]
        public async Task EventRemoveCommand(string title)
        {

        }

        [Command("events")]
        public async Task EventsCommand()
        {

        }
    }
}
