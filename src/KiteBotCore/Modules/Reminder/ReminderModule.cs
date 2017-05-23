using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace KiteBotCore.Modules.Reminder
{
    public class ReminderModule : ModuleBase
    {
        public ReminderService ReminderService { get; set; }
        private static readonly Regex Regex = new Regex(@"(?<digits>\d+)\s+?(?<unit>\w+)(?:\s+(?<reason>[\w\d\s':/`\\\.,!?]+))?");

        [Command("reminder")]
        [Alias("remindme")]
        [Summary("Adds an event that will DM you at a specified day/hour/minute/second in the future")]
        public async Task AddReminderEventCommand([Remainder] string message)
        {
            Match matches = Regex.Match(message);
            if (matches.Success)
            {
                var milliseconds = 0;
                switch (matches.Groups["unit"].Value.ToLower()[0])
                {
                    case 's':
                        milliseconds = int.Parse(matches.Groups["digits"].Value)*1000;
                        break;
                    case 'm':
                        milliseconds = int.Parse(matches.Groups["digits"].Value)*1000*60;
                        break;
                    case 'h':
                        milliseconds = int.Parse(matches.Groups["digits"].Value)*1000*60*60;
                        break;
                    case 'd':
                        milliseconds = int.Parse(matches.Groups["digits"].Value)*1000*60*60*24;
                        break;
                    default:
                        await
                            ReplyAsync("Couldn't find any supported time units, please use [seconds|minutes|hour|days]").ConfigureAwait(false);
                        break;
                }

                var reminderEvent = new ReminderEvent
                {
                    RequestedTime = DateTime.Now.AddMilliseconds(milliseconds),
                    UserId = Context.User.Id,
                    Reason = matches.Groups["reason"].Success ? matches.Groups["reason"].Value : "No specified reason"
                };

                ReminderService.AddReminder(reminderEvent);

                await
                    ReplyAsync(
                        $"Reminder set for {reminderEvent.RequestedTime.ToUniversalTime().ToString("g", new CultureInfo("en-US"))} UTC with reason: {reminderEvent.Reason}").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("Couldn't parse your command, please use the format \"!Reminder [number] [seconds|minutes|hour|days] [optional: reason for reminder]\"").ConfigureAwait(false);
            }
            
        }
    }
}
