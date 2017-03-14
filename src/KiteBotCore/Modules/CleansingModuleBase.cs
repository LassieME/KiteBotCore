using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace KiteBotCore.Modules
{
    public abstract class CleansingModuleBase : ModuleBase
    {
        protected override async Task<IUserMessage> ReplyAsync(string message, bool isTTS = false, Embed embed = null, RequestOptions options = null)
        {
            Regex regex = new Regex("<@!([0-9]+)>");
            string output = message.Replace("@​everyone", "@every\x200Bone").Replace("@here", "@he\x200Bre");
            
            foreach (Match match in regex.Matches(message))
            {
                var user = await Context.Guild.GetUserAsync(Convert.ToUInt64(match.Groups[0].Value)).ConfigureAwait(false);
                if (user != null)
                    output = output.Replace(match.Value, "@" + (user.Nickname ?? user.Username));
            }
            return await Context.Channel.SendMessageAsync(output, isTTS, embed, options).ConfigureAwait(false);
        }
    }
}
