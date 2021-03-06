﻿using System;
using System.Linq;

namespace KiteBotCore.Utils.FuzzyString
{
    public static partial class ComparisonMetrics
    {
        public static int HammingDistance(this string source, string target)
        {
            int distance = 0;

            if (source.Length == target.Length)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    if (source[i] != target[i])
                    {
                        distance++;
                    }
                }
                return distance;
            }
            else { return 99999; }
        }
    }
}
