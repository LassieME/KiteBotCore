using System;
using Discord.Addons.Interactive;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;
using Discord;
using System.Threading.Tasks;

namespace KiteBotCore
{
    public class MyInteractiveService : InteractiveService
    {
        public MyInteractiveService(DiscordSocketClient discord, TimeSpan? defaultTimeout = null) : base(discord, new InteractiveServiceConfig() { DefaultTimeout = (TimeSpan)defaultTimeout})
        {
        }

        public new void AddReactionCallback(IMessage message, IReactionCallback callback)
        {
            base.AddReactionCallback(message, callback);
            if(callback.Timeout != null) _ = Task.Run(() => { Task.Delay(callback.Timeout.Value); RemoveReactionCallback(message); });
        }
    }
}
