using System;

namespace KiteBotCore.Utils.FuzzyString
{
    public static partial class ComparisonMetrics
    {
        public static int LevenshteinDistance(this string source, string target)
        {
            if (string.IsNullOrEmpty(source))
            {
                return !string.IsNullOrEmpty(target) ? target.Length : 0;
            }

            if (string.IsNullOrEmpty(target))
            {
                return !string.IsNullOrEmpty(source) ? source.Length : 0;
            }

            int[,] d = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= d.GetUpperBound(0); i += 1)
            {
                d[i, 0] = i;
            }

            for (int i = 0; i <= d.GetUpperBound(1); i += 1)
            {
                d[0, i] = i;
            }

            for (int i = 1; i <= d.GetUpperBound(0); i += 1)
            {
                for (int j = 1; j <= d.GetUpperBound(1); j += 1)
                {
                    int cost = Convert.ToInt32(source[i - 1] != target[j - 1]);

                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }

            return d[d.GetUpperBound(0), d.GetUpperBound(1)];

        }
        public static int LevenshteinDistanceBugged(this string source, string target)
        {
            if (source.Length == 0) { return target.Length; }
            if (target.Length == 0) { return source.Length; }

            int distance = 0;

            distance = source[source.Length - 1] == target[target.Length - 1] ? 0 : 1;

            return Math.Min(
                Math.Min(
                    LevenshteinDistance(source.Substring(0, source.Length - 1), target) + 1,
                    LevenshteinDistance(source, target.Substring(0, target.Length - 1))) + 1,
                LevenshteinDistance(source.Substring(0, source.Length - 1), target.Substring(0, target.Length - 1)) + distance);
        }

        public static double NormalizedLevenshteinDistance(this string source, string target)
        {
            int unnormalizedLevenshteinDistance = source.LevenshteinDistance(target);

            return unnormalizedLevenshteinDistance - source.LevenshteinDistanceLowerBounds(target);
        }

        public static int LevenshteinDistanceUpperBounds(this string source, string target)
        {
            // If the two strings are the same length then the Hamming Distance is the upper bounds of the Levenshtien Distance.
            if (source.Length == target.Length) { return source.HammingDistance(target); }

            // Otherwise, the upper bound is the length of the longer string.
            if (source.Length > target.Length) { return source.Length; }
            if (target.Length > source.Length) { return target.Length; }

            return 9999;
        }

        public static int LevenshteinDistanceLowerBounds(this string source, string target)
        {
            // If the two strings are the same length then the lower bound is zero.
            if (source.Length == target.Length) { return 0; }

            // If the two strings are different lengths then the lower bounds is the difference in length.
            return Math.Abs(source.Length - target.Length);
        }

    }
}
