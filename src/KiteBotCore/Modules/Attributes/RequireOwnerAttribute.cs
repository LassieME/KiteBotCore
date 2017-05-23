using System;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json;
using Microsoft.Extensions.DependencyInjection;

namespace KiteBotCore.Modules
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    // Inherit from PreconditionAttribute
    public class RequireOwnerAttribute : PreconditionAttribute
    {
        public static ulong OwnerId;

        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo executingCommand, IServiceProvider map)
        {
            var settings = map.GetService<BotSettings>();
            return Task.FromResult(context.User.Id == settings.OwnerId ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("You must be the owner of the bot."));
        }
    }
}
