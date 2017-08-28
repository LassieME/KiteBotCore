using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord.Commands;
using Serilog;

namespace KiteBotCore.Modules
{
    public class Sandwich : ModuleBase
    {
        public Random RandomSeed { get; set; }

        private Stopwatch _stopwatch;
        protected override void BeforeExecute(CommandInfo command)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute(CommandInfo command)
        {
            _stopwatch.Stop();
            Log.Debug($"Sandwich Command: {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }

        internal readonly List<string> BreadTypes = new List<string>
        {
            "White Bread", "Wheat Bread", "Rye Bread", "Multi-Grain Bread", "Hoagie Roll", "Baguette", "French Bread",
            "Whole Grain Tortilla Wrap", "Ciabatta", "Sour Dough Roll", "Flatbread"
        };
        internal readonly List<string> MeatTypes = new List<string>
        {
            "Capicola", "Sliced Chicken Breast", "Salami", "Pepperoni", "Roasted Turkey", "Roast Beef", "Ham", "Bacon", "Bologna", "Pastrami",
            "Corned Beef", "Honey Ham", "Smoked Turkey"
        };
        internal readonly List<string> CheeseTypes = new List<string>
        {
            "Cheddar", "Mozerella", "American", "Swiss", "Havarti", "Muenster", "Gruyere", "Pepper Jack"
        };
        internal readonly List<string> VeggieTypes = new List<string>
        {
            "Lettuce", "Tomato", "Onion", "Bell Pepper", "Black Olive", "Cucumber", "Pickles", "Hot Peppers", "Spinach", "Avocado"
        };
        internal readonly List<string> DressingTypes = new List<string>
        {
            "Mayo", "Mustard", "Oil and Vinegar", "Olive Oil", "Italian Dressing", "Home-Made Special Sauce", "Dijon", "Pesto"
        };
        internal readonly List<string> SpecialInstructions = new List<string>
        {
            "Grilled", "Toasted", "Panini Press", "Double Stack", "Triple Stack", "Foot Long", "Open Faced"
        };
        internal readonly List<string> CategoryList = new List<string>
        {
            "Bread: ", "Meat: ", "Cheese: ", "Toppings: ", "Dressing: "
        };

        [Command("sandwich")]
        [Summary("Makes a sandwich suggestion.")]
        public async Task SandwichCommand()
        {
            var nl = Environment.NewLine;
            string builtSandwich = Context.User.Username + " check out this sandwich:" + nl;

            int categoryTracker = 0;

            List<List<string>> optionLists = new List<List<string>>();

            optionLists.AddRange(new [] { BreadTypes, MeatTypes, CheeseTypes, VeggieTypes, DressingTypes });

            foreach (List<string> currentList in optionLists)
            {
                int qty = RandomSeed.Next(1, 3);

                //only 1 bread
                if (categoryTracker == 0)
                    qty = 1;

                //add 2 additional veggies
                if (categoryTracker == 3)
                {
                    qty += 2;
                }

                if (qty > 0)
                {
                    builtSandwich += CategoryList[categoryTracker];

                    for (int i = 1; i <= qty; i++)
                    {
                        int rand = RandomSeed.Next(0, currentList.Count);

                        //pull new random items until one not in the response is found
                        while (0 <= builtSandwich.IndexOf(currentList[rand], 0))
                        {
                            rand = RandomSeed.Next(0, currentList.Count);
                        }

                        builtSandwich += currentList[rand];

                        if (i == qty)
                        {
                            builtSandwich += ".";
                        }

                        else
                        {
                            builtSandwich += ", ";
                        }
                    }

                    categoryTracker++;
                    builtSandwich += nl;
                }

                else categoryTracker++;
            }

            builtSandwich += "Special Instructions: " + SpecialInstructions[RandomSeed.Next(0, SpecialInstructions.Count)];

            await ReplyAsync(builtSandwich);
        }
    }
}
