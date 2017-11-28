using Discord.Addons.InteractiveCommands;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Example.Modules.Injected
{
    public class InjectedModule : ModuleBase
    {
        private readonly InteractiveService _interactive;

        public InjectedModule(InteractiveService interactive)
        {
            _interactive = interactive;
        }

        [Command("favnum")]
        public async Task FavoriteNumber()
        {
            await ReplyAsync("What is your favorite number?");
            var response = await _interactive.WaitForMessage(Context.User, Context.Channel);
            await ReplyAsync($"Your favorite number is {response.Content}");
        }
    }
}
