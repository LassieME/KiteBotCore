using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord.Commands;
using Serilog;

namespace KiteBotCore.Modules
{
    public class Food : ModuleBase
    {
        public static string MealFileLocation = Directory.GetCurrentDirectory() + "/Content/Meals.txt";
        private static readonly string[] MealResponses = File.ReadAllLines(MealFileLocation);
        public Random Random { get; set; }

        private Stopwatch _stopwatch;
        protected override void BeforeExecute(CommandInfo command)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute(CommandInfo command)
        {
            _stopwatch.Stop();
            Log.Debug($"Food Command: {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }

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