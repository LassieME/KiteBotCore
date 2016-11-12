using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace KiteBotCore.Modules
{
    public abstract class CleansingModuleBase : ModuleBase
    {
        protected override async Task<IUserMessage> ReplyAsync(string message, bool isTTS = false, RequestOptions options = null)
        {
            var output = message.Replace("@​everyone", "@every\x200Bone").Replace("@here", "@he\x200Bre");
            return await Context.Channel.SendMessageAsync(output, isTTS, options).ConfigureAwait(false);
        }
    }
}
