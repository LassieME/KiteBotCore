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
        public Task EventAddCommand(string title, DateTimeOffset dateTime, [Remainder]string description)
        {
            return Task.CompletedTask;
        }

        [Command("event remove")]
        public Task EventRemoveCommand(string title)
        {
            return Task.CompletedTask;
        }

        [Command("events")]
        public Task EventsCommand()
        {
            return Task.CompletedTask;
        }
    }
}
