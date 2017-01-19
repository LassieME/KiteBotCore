using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Discord.API;

namespace KiteBotCore.Modules
{
    [Group("inspect")]
    public class Inspect : ModuleBase
    {
        private readonly CommandService _handler;
        private readonly IDependencyMap _map;

        public Inspect(IDependencyMap map)
        {
            _handler = map.Get<CommandService>();
            _map = map;
        }

        [Command("role")]        
        [Summary("Lists properties of a given role")]
        [RequireOwner]
        public async Task RoleCommand([Remainder] string input)
        {
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == input);
            var output = $"Name: {role.Name}\n" +
                $"Position: {role.Position}\n" +
                $"Color: R:{role.Color.R}, G:{role.Color.G}, B:{role.Color.B}, Raw:{role.Color.RawValue:X}\n" +
                $"IsHoisted: {role.IsHoisted}\n" +
                $"IsManaged: {role.IsManaged}\n" +
                $"IsMentionable: {role.IsMentionable}\n" +
                $"Permissions: {string.Join(",",role.Permissions.ToList())}";

            await ReplyAsync(output);
        }
    }
}