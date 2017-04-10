using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using KiteBotCore.Json;

namespace KiteBotCore.Modules
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    // Inherit from PreconditionAttribute
    public class RequireServerAttribute : PreconditionAttribute
    {
        private readonly Server[] _requiredServers;

        public RequireServerAttribute(params Server[] requiredservers)
        {
            _requiredServers = requiredservers;
        }

        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo executingCommand, IDependencyMap map)
        {
            if (context.Guild != null)
            {
                return Task.FromResult(_requiredServers.Any(x => (Server)context.Guild.Id == x) ?
                    PreconditionResult.FromSuccess() : PreconditionResult.FromError("You are in a server that does not have this command enabled."));
            }
            else if(map.Get<DiscordSocketClient>().Guilds.Any(g => _requiredServers.Contains((Server)g.Id) && g.Users.Any(u => u.Id == context.User.Id)))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            return Task.FromResult(PreconditionResult.FromError("You must be in a server that has this command enabled."));
        }
    }

    public enum Server : ulong
    {
        KiteCo = 85814946004238336,
        GiantBomb = 106386929506873344
    }
}
