using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Utils;

namespace KiteBotCore.Modules
{
    public class DiceRoller : ModuleBase
    {
        public CryptoRandom Random { get; set; }

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
                        await ReplyAsync("Why are you doing this, too many dice.");

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
                        await ReplyAsync(resultsString + $" + {constant} = {result + constant}");
                    }
                    await ReplyAsync(resultsString);
                }
                else if (matches.Groups["single"].Success)
                {
                    await ReplyAsync(Random.Next(1, int.Parse(matches.Groups["single"].Value)).ToString());
                }
                else
                {
                    await ReplyAsync("use the format 5d6, d6 or simply specify a positive integer");
                }
            }
            catch (OverflowException)
            {
                await ReplyAsync("Why are you doing this? You're on my shitlist now.");
            }
        }
    }
}