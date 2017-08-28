using Discord.Commands;
using KiteBotCore.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KiteBotCore.Modules
{
    public class DiceRoller : ModuleBase
    {
        public CryptoRandom Random { get; set; }

        private Stopwatch _stopwatch;
        protected override void BeforeExecute(CommandInfo command)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute(CommandInfo command)
        {
            _stopwatch.Stop();
            Log.Debug($"Dice Command: {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }

        [Command("roll")]
        [Alias("rolldice")]
        [Summary("rolls some dice, use 2d10 format")]
        public async Task DiceRoll([Remainder] string text)
        {
            var diceroll = new Regex(
                @"(?<dice>[0-9]+)d(?<sides>[0-9]+)(\+(?<constant>[0-9]+))?|d?(?<single>[0-9]+)"); //roll 2d20+20
            Match matches = diceroll.Match(text);
            var result = 0;
            try
            {
                if (matches.Groups["dice"].Success && matches.Groups["sides"].Success)
                {
                    int dice = int.Parse(matches.Groups["dice"].Value);
                    int sides = int.Parse(matches.Groups["sides"].Value);

                    if (dice > 20)
                        await ReplyAsync("Why are you doing this, too many dice.").ConfigureAwait(false);

                    var resultsHistory = new List<int>();

                    for (var i = 0; i < dice; i++)
                        resultsHistory.Add(Random.Next(1, sides));

                    string resultsString = null;
                    var counter = 0;
                    foreach (int i in resultsHistory)
                    {
                        resultsString += i.ToString();
                        result += i;

                        counter++;
                        if (counter < resultsHistory.Count)
                            resultsString += " + ";
                    }

                    resultsString += " = " + result;
                    if (matches.Groups["constant"].Success)
                    {
                        int constant = int.Parse(matches.Groups["constant"].Value);
                        await ReplyAsync(resultsString + $" + {constant} = {result + constant}").ConfigureAwait(false);
                    }
                    await ReplyAsync(resultsString).ConfigureAwait(false);
                }
                else if (matches.Groups["single"].Success)
                {
                    await ReplyAsync(Random.Next(1, int.Parse(matches.Groups["single"].Value)).ToString()).ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync("use the format 5d6, d6 or simply specify a positive integer").ConfigureAwait(false);
                }
            }
            catch (OverflowException)
            {
                await ReplyAsync("Why are you doing this? You're on my shitlist now.").ConfigureAwait(false);
            }
        }
    }
}