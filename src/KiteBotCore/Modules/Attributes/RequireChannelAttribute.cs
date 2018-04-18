using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json;
using Microsoft.Extensions.DependencyInjection;

namespace KiteBotCore.Modules
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    // Inherit from PreconditionAttribute
    public class RequireChannelAttribute : PreconditionAttribute
    {
        private readonly ulong[] _channels;
        public RequireChannelAttribute(params ulong[] channels)
        {
            _channels = channels;
        }

        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo executingCommand,
            IServiceProvider map)
        {
            if (_channels.Contains(context.Channel.Id))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            return Task.FromResult(PreconditionResult.FromError("You must be in #bot-linkdump to use this command"));
        }
    }
}
