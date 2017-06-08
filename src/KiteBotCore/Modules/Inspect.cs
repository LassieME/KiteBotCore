using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace KiteBotCore.Modules
{
    [Group("inspect")]
    public class Inspect : ModuleBase //TODO: Either expand this via more commands, or make one command that uses reflection to find properties
    {
        [Command("role")]        
        [Summary("Lists properties of a given role")]
        [RequireBotOwner]
        public async Task RoleCommand([Remainder] IRole role)
        {
            var output = $"Name: {role.Name}\n" +
                $"Position: {role.Position}\n" +
                $"Color: R:{role.Color.R}, G:{role.Color.G}, B:{role.Color.B}, Raw:{role.Color.RawValue:X}\n" +
                $"IsHoisted: {role.IsHoisted}\n" +
                $"IsManaged: {role.IsManaged}\n" +
                $"IsMentionable: {role.IsMentionable}\n" +
                $"Permissions: {string.Join(",",role.Permissions.ToList())}";

            await ReplyAsync(output).ConfigureAwait(false);
        }
    }
}