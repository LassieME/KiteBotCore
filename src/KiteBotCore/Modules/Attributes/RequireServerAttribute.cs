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
                    PreconditionResult.FromSuccess() : PreconditionResult.FromError("You must be in the right server."));
            }
            else
            {
                foreach (var server in _requiredServers)
                {
                    var guild = map.Get<DiscordSocketClient>().GetGuild((ulong)server);
                    bool isInGuild = guild.Users.Any(x => x.Id == context.User.Id);
                    if (isInGuild)
                        return Task.FromResult(PreconditionResult.FromSuccess());
                }
                
                return Task.FromResult(PreconditionResult.FromError("You must be in a server that has this command enabled."));
            }
        }
    }

    public enum Server : ulong
    {
        KiteCo = 85814946004238336,
        GiantBomb = 106386929506873344
    }
}
