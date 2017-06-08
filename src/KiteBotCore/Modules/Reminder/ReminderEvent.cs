using System;
using System.Collections.Generic;
using System.Text;

namespace KiteBotCore.Modules.Reminder
{
    public struct ReminderEvent
    {
        public DateTime RequestedTime { get; set; }
        public ulong UserId { get; set; }
        public string Reason { get; set; }
    }
}
