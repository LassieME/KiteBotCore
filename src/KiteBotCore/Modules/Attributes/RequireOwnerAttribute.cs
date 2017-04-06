﻿using System;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json;

namespace KiteBotCore.Modules
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    // Inherit from PreconditionAttribute
    public class RequireOwnerAttribute : PreconditionAttribute
    {
        public static ulong OwnerId;

        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo executingCommand, IDependencyMap map)
        {
            if (map.TryGet(out BotSettings botSettings))
            {
                OwnerId = botSettings.OwnerId;
            }
            else
            {
                Console.WriteLine("IDependencyMap did not contain anything.");
            }
            // If the author of the message is '66078337084162048', return success; otherwise fail. 
            return Task.FromResult(context.User.Id == OwnerId ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("You must be the owner of the bot."));
        }
    }
}