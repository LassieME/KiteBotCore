using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace KiteBotCore.Modules
{
    public abstract class CleansingModuleBase : ModuleBase
    {
        protected override async Task<IUserMessage> ReplyAsync(string message, bool isTts = false, Embed embed = null, RequestOptions options = null)
        {
            Regex regex = new Regex("<@!?([0-9]+)>");
            string output = message.Replace("@​everyone", "@every\x200Bone").Replace("@here", "@he\x200Bre");
            
            foreach (Match match in regex.Matches(message))
            {
                var user = await Context.Client.GetUserAsync(Convert.ToUInt64(match.Groups[1].Value)).ConfigureAwait(false);
                if (user != null)
                    output = output.Replace(match.Value, "@\x200B" + ((user as IGuildUser)?.Nickname ?? user.Username));
            }
            return await Context.Channel.SendMessageAsync(output, isTts, embed, options).ConfigureAwait(false);
        }
    }
}
