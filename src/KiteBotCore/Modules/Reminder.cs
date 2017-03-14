using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Newtonsoft.Json;

namespace KiteBotCore.Modules
{
    public class ReminderModule : ModuleBase
    {
        private static readonly Regex Regex = new Regex(@"(?<digits>\d+)\s+(?<unit>\w+)(?:\s+(?<reason>[\w\d\s':/`\\\.,!?]+))?");

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

                var reminderEvent = new ReminderService.ReminderEvent
                {
                    RequestedTime = DateTime.Now.AddMilliseconds(milliseconds),
                    UserId = Context.User.Id,
                    Reason = matches.Groups["reason"].Success ? matches.Groups["reason"].Value : "No specified reason"
                };

                if (ReminderService.ReminderList.Count == 0)
                {
                    ReminderService.ReminderList.AddFirst(reminderEvent);
                    ReminderService.SetTimer(reminderEvent.RequestedTime);
                }
                else
                {
                    var laternode =
                        ReminderService.ReminderList.EnumerateNodes()
                            .FirstOrDefault(x => x.Value.RequestedTime.CompareTo(reminderEvent.RequestedTime) > 0);
                    if (laternode == null)
                    {
                        ReminderService.ReminderList.AddLast(reminderEvent);
                    }
                    else
                    {
                        ReminderService.ReminderList.AddBefore(laternode, reminderEvent);
                    }
                }
                ReminderService.Save();
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

    public static class ReminderService
    {
        public static string RootDirectory = Directory.GetCurrentDirectory();
        public static string ReminderPath => RootDirectory + "/Content/ReminderList.json";

        internal static Timer ReminderTimer;
        internal static readonly LinkedList<ReminderEvent> ReminderList = File.Exists(ReminderPath) ?
                JsonConvert.DeserializeObject<LinkedList<ReminderEvent>>(File.ReadAllText(ReminderPath)) :
                new LinkedList<ReminderEvent>();

        static ReminderService()
        {
            List<ReminderEvent> deleteBuffer = new List<ReminderEvent>(ReminderList.Count);
            foreach (var reminder in ReminderList)
            {
                if (ReminderList.First.Value.RequestedTime <= DateTime.Now)
                {
                    deleteBuffer.Add(reminder);
                }
            }
            DeleteList(deleteBuffer);
            if (ReminderList.Count != 0)
            {
                SetTimer(ReminderList.First.Value.RequestedTime);
            }
        }

        internal static void SetTimer(DateTime newTimer)
        {
            TimeSpan interval = newTimer - DateTime.Now;
            ReminderTimer?.Dispose();
            ReminderTimer = new Timer(CheckReminders, null, interval, TimeSpan.FromMinutes(1));
            
        }

        private static async Task CheckReminders()
        {
            List<ReminderEvent> deleteBuffer = new List<ReminderEvent>(ReminderList.Count);

            foreach (ReminderEvent reminder in ReminderList)
            {
                if (reminder.RequestedTime.CompareTo(DateTime.Now) <= 0)
                {
                    var channel = await Program.Client.GetUser(reminder.UserId).CreateDMChannelAsync().ConfigureAwait(false);

                    await channel.SendMessageAsync($"Reminder: {reminder.Reason}").ConfigureAwait(false);

                    deleteBuffer.Add(reminder);
                    if (ReminderList.Count == 0)
                    {
                        ReminderTimer.Dispose();
                        break;
                    }
                }
            }
            DeleteList(deleteBuffer);
            if (ReminderList.Count != 0)
            {
                SetTimer(ReminderList.First.Value.RequestedTime);
            }
            Save();
        }

        private static void DeleteList(List<ReminderEvent> deleteBuffer)
        {
            foreach (var lapsedEvent in deleteBuffer)
            {
                ReminderList.Remove(lapsedEvent);
            }
        }

        private static async void CheckReminders(object sender)
        {
            try
            {
                await CheckReminders().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + ex.Message);
            }
        }

        internal static void Save()
        {
            File.WriteAllText(ReminderPath,JsonConvert.SerializeObject(ReminderList));
        }

        internal struct ReminderEvent
        {
            public DateTime RequestedTime { get; set; }
            public ulong UserId { get; set; }
            public string Reason { get; set; }
        }

        public static bool Init()
        {
            return true;
        }
    }
    public static class LinkedListExtensions
    {
        public static IEnumerable<LinkedListNode<T>> EnumerateNodes<T>(this LinkedList<T> list)
        {
            var node = list.First;
            while (node != null)
            {
                yield return node;
                node = node.Next;
            }
        }
    }
}
