using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.InteractiveCommands;
using Discord.Commands;
using Discord.WebSocket;

namespace Example.Modules.Test
{
    public class TestModule : InteractiveModuleBase
    {
        [Command("sayhello", RunMode = RunMode.Async)]
        public async Task SayHello()
        {
            await ReplyAsync("What is your name?");
            var response = await WaitForMessage(Context.Message.Author, Context.Channel);
            await ReplyAsync($"Hello, {response.Content}");
        }

        [Command("favoriteanimal", RunMode = RunMode.Async)]
        public async Task FavoriteAnimal()
        {
            await ReplyAsync("What is your favorite animal?");
            var response = await WaitForMessage(Context.Message.Author, Context.Channel, null, new MessageContainsResponsePrecondition("dog", "cat", "giraffe"));
            await ReplyAsync($"Your favorite animal is a {response.Content}!");
        }

        [Command("destroy")]
        public async Task DeleteAfter()
        {
            await ReplyAsync("This message will destroy itself in 5 seconds", deleteAfter: 5);
        }
    }
}
