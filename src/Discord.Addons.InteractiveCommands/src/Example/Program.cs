﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Example
{
    public class Program
    {
        public static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        private DiscordSocketClient client;

        public async Task Start()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 1000,
            });

            string token = Environment.GetEnvironmentVariable("discord-foxboat-token");

            client.Log += (msg) =>
            {
                Console.WriteLine(msg.ToString());
                return Task.CompletedTask;
            };

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            var map = new DependencyMap();
            ConfigureServices(map);
            await new CommandHandler().Install(map);

            await Task.Delay(-1);
        }

        public void ConfigureServices(IDependencyMap map)
        {
            map.Add(client);
        }
    }
}
