using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MarkovSharp;
using MarkovSharp.Models;
using MarkovSharp.TokenisationStrategies;

namespace MarkovSharpCore.TokenisationStrategies
{
    public class ReversableStringMarkov : StringMarkov
    {
        public ReversableStringMarkov(int level = 2)
            : base(level)
        { }

        public string WalkBothWays(string seed = "")
        {
            if (string.IsNullOrEmpty(seed))
            {
                throw new ArgumentException("You fucked up");
            }

            var preSentence = seed;

            var list = Model.GetKeyByValue(SplitTokens(seed).Last());
            
            while (list.Count > 0)
            {
                var randomPick = list[RandomGenerator.Next(list.Count)];
                preSentence = string.Join(" ", string.Join(" ", randomPick.Key.Before), preSentence);
                if (randomPick.Key.Before.Any(x => x == ""))
                    break;
                list = Model.GetKeyByValue(SplitTokens(preSentence).First());
            }

            var sentence = preSentence;

            return Walk(1, sentence).First();
        }
    }

    public static class DictionaryExtensions
    {
        public static List<KeyValuePair<SourceGrams<string>, List<string>>> GetKeyByValue(this ConcurrentDictionary<SourceGrams<string>, List<string>> dict, string value)
        {
            return dict.Where(x => x.Value.Contains(value)).ToList();
        }
    }
}
