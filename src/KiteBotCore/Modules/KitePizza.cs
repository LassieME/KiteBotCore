using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace KiteBotCore.Modules
{
    public class KitePizza : ModuleBase
    {
        readonly Random _randomSeed = new Random();

        [Command("pizza")]
        [Summary("Makes a pizza suggestion.")]
        public async Task Pizza(string optional = null)
        {
            List<string> pizzaToppings = new List<string>();

            if ( optional != null && ((Context.User.Id == 85967975382786048 && !optional.ToLower().Contains("opt-out")) || optional.ToLower().Contains("japan")))
            {
                pizzaToppings.AddRange(new [] {"Mayonnaise", "Squid", "Raw Tuna", "Raw Salmon", "Avocado", "Squid Ink",
                    "Broccoli", "Shrimp", "Teriyaki Chicken", "Bonito Flakes", "Hot Sake",
                    "Soft Tofu", "Sushi Rice", "Nori", "Corn", "Snow Peas", "Bamboo Shoots",
                    "Potato", "Onion"});
            }

            else
                pizzaToppings.AddRange(new [] {"Extra Cheese", "Pepperoni", "Sausage", "Chicken", "Ham", "Canadian Bacon",
                    "Bacon", "Green Peppers", "Black Olives", "White Onion", "Diced Tomatoes", "Pesto",
                    "Spinach", "Roasted Red Peppers", "Sun Dried Tomato", "Pineapple", "Italian Sausage",
                    "Red Onion", "Green Chile", "Basil", "Mushrooms", "Beef"});

            int numberOfToppings = _randomSeed.Next(2, 7);//2 is 3, 7 is 8

            string buildThisPizza = "&USER you should put these things in the pizza: ";

            for (int i = 0; i <= numberOfToppings; i++)
            {
                int j = _randomSeed.Next(0, pizzaToppings.Count);
                buildThisPizza += pizzaToppings[j];
                pizzaToppings.Remove(pizzaToppings[j]);

                if (i == numberOfToppings)
                {
                    buildThisPizza += ".";
                }
                else
                {
                    buildThisPizza += ", ";
                }
            }

            await ReplyAsync(buildThisPizza.Replace("&USER", Context.User.Username));
        }

    }
}
