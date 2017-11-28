using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Addons.InteractiveCommands
{
    public struct ResponseContext
    {
        public IDiscordClient Client { get; }
        public IGuild Guild { get; }
        public IMessageChannel Channel { get; }
        public IUser User { get; }
        public IUserMessage Response { get; }
        public bool IsPrivate => Channel is IPrivateChannel;

        internal ResponseContext(IDiscordClient client, IGuild guild, IMessageChannel channel, IUser user, IUserMessage msg)
        {
            Client = client;
            Guild = guild;
            Channel = channel;
            User = user;
            Response = msg;
        }
        internal ResponseContext(IDiscordClient client, IUserMessage msg)
        {
            Client = client;
            Guild = (msg.Channel as IGuildChannel)?.Guild;
            Channel = msg.Channel;
            User = msg.Author;
            Response = msg;
        }

    }
}
