using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace KiteBotCore.Modules.Reminder
{
    public class ReminderService
    {
        public static string ReminderPath => Directory.GetCurrentDirectory() + "/Content/ReminderList.json";

        private Timer _reminderTimer;
        private readonly LinkedList<ReminderEvent> _reminderList = File.Exists(ReminderPath) ?
            JsonConvert.DeserializeObject<LinkedList<ReminderEvent>>(File.ReadAllText(ReminderPath)) :
            new LinkedList<ReminderEvent>();

        private readonly DiscordSocketClient _client;

        public ReminderService(DiscordSocketClient client)
        {
            _client = client;

            //Remove all expired reminders that happened while the bot was offline
            List<ReminderEvent> deleteBuffer = new List<ReminderEvent>(_reminderList.Count);
            foreach (var reminder in _reminderList)
            {
                if (_reminderList.First.Value.RequestedTime <= DateTime.Now)
                {
                    deleteBuffer.Add(reminder);
                }
            }
            DeleteList(deleteBuffer);
            if (_reminderList.Count != 0)
            {
                SetTimer(_reminderList.First.Value.RequestedTime);
            }
        }

        private void SetTimer(DateTime newTimer)
        {
            TimeSpan interval = newTimer - DateTime.Now;
            if (_reminderTimer != null)
            {
                _reminderTimer.Change(interval, TimeSpan.FromMilliseconds(-1));
            }
            else
            {
                _reminderTimer = new Timer(CheckReminders, null, interval, TimeSpan.FromMilliseconds(-1));
            }
        }

        public void AddReminder(ReminderEvent reminderEvent)
        {
            if (_reminderList.Count == 0)
            {
                _reminderList.AddFirst(reminderEvent);
                SetTimer(reminderEvent.RequestedTime);
            }
            else
            {
                var laternode = _reminderList.EnumerateNodes()
                    .FirstOrDefault(x => x.Value.RequestedTime.CompareTo(reminderEvent.RequestedTime) > 0);
                if (laternode == null)
                {
                    _reminderList.AddLast(reminderEvent);
                }
                else
                {
                    _reminderList.AddBefore(laternode, reminderEvent);
                }
            }
            Save();
        }

        private async Task CheckReminders()
        {
            List<ReminderEvent> deleteBuffer = new List<ReminderEvent>(_reminderList.Count);

            foreach (ReminderEvent reminder in _reminderList)
            {
                if (reminder.RequestedTime.CompareTo(DateTime.Now) <= 0)
                {
                    var channel = await _client.GetUser(reminder.UserId).CreateDMChannelAsync().ConfigureAwait(false);

                    await channel.SendMessageAsync($"Reminder: {reminder.Reason}").ConfigureAwait(false);

                    deleteBuffer.Add(reminder);
                    if (_reminderList.Count == 0)
                    {
                        _reminderTimer.Dispose();
                        break;
                    }
                }
            }
            DeleteList(deleteBuffer);
            if (_reminderList.Count != 0)
            {
                SetTimer(_reminderList.First.Value.RequestedTime);
            }
            Save();
        }

        private void DeleteList(List<ReminderEvent> deleteBuffer)
        {
            foreach (var lapsedEvent in deleteBuffer)
            {
                _reminderList.Remove(lapsedEvent);
            }
        }

        private async void CheckReminders(object sender)
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

        private void Save()
        {
            File.WriteAllText(ReminderPath, JsonConvert.SerializeObject(_reminderList));
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
