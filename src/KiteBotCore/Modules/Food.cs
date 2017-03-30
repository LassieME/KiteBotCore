﻿using System;
using System.IO;
using System.Threading.Tasks;
using Discord.Commands;

namespace KiteBotCore.Modules
{
    public class Food : ModuleBase
    {
        public static string MealFileLocation = Directory.GetCurrentDirectory() + "/Content/Meals.txt";
        private static readonly string[] MealResponses = File.ReadAllLines(MealFileLocation);
        public Random Random { get; set; }

        [Command("dinner")]
        [Alias("lunch", "meal", "food")]
        [Summary("Suggests dinner or lunch.")]
        public async Task FoodyCommand()
        {
            await ReplyAsync(MealResponses[Random.Next(0, MealResponses.Length)]
                .Replace("USER", Context.User.Username));
        }
    }
}