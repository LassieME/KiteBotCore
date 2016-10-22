using System;
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
        private readonly Server _requiredServer;

        public RequireServerAttribute(Server requiredserver)
        {
            _requiredServer = requiredserver;
        }

        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissions(CommandContext context, CommandInfo executingCommand, IDependencyMap map)
        {
            if (context.Guild != null)
            {
                return Task.FromResult((Server)context.Guild.Id == _requiredServer ?
                    PreconditionResult.FromSuccess() : PreconditionResult.FromError("You must be in the right server."));
            }
            else
            {
                var guild = map.Get<DiscordSocketClient>().GetGuild((ulong) _requiredServer);
                return Task.FromResult(guild != null && guild.Users.Any(x => x.Id == context.User.Id) ?
                    PreconditionResult.FromSuccess() : PreconditionResult.FromError("You must be in the right server."));
            }
        }
    }

    public enum Server : ulong
    {
        KiteCo = 85814946004238336,
        GiantBomb = 106386929506873344
    }
}
