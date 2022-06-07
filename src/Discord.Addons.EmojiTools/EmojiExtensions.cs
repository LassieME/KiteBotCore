﻿using System;
using System.Linq;

namespace Discord.Addons.EmojiTools
{
    public static class EmojiExtensions
    {
        /// <summary>
        /// Return a Unicode Emoji given a shorthand alias
        /// </summary>
        /// <param name="text">A shorthand alias for the emoji, e.g. :race_car:</param>
        /// <returns>A unicode emoji, for direct use in a reaction or message.</returns>
        public static Emoji FromText(string text)
        {
            text = text.Trim(':');

            var unicode = default(string);
            if (EmojiMap.Map.TryGetValue(text, out unicode))
                return new Emoji(unicode);
            throw new ArgumentException("The given alias could not be matched to a Unicode Emoji.", nameof(text));
        }
        /// <summary>
        /// Returns the shorthand alias for a given emoji.
        /// </summary>
        /// <param name="emoji">A unicode emoji.</param>
        /// <returns>A shorthand alias for the emoji, e.g. :race_car:</returns>
        /// <exception cref="System.Exception">If the emoji does not have a mapping, an exception will be thrown.</exception>
        public static string GetShorthand(this Emoji emoji)
        {
            var key = EmojiMap.Map.FirstOrDefault(x => x.Value == emoji.Name).Key;
            if (String.IsNullOrEmpty(key))
                throw new Exception($"Could not find an emoji with value '{emoji.Name}'");
            return String.Concat(":", key, ":");
        }
        /// <summary>
        /// Attempts to return the shorthand alias for a given emoji.
        /// </summary>
        /// <param name="emoji">A unicode emoji.</param>
        /// <param name="shorthand">A string reference, where the shorthand alias for the emoji will be placed.</param>
        /// <returns>True if the emoji was found, false if it was not.</returns>
        public static bool TryGetShorthand(this Emoji emoji, out string shorthand)
        {
            var key = EmojiMap.Map.FirstOrDefault(x => x.Value == emoji.Name).Key;
            if (String.IsNullOrEmpty(key))
            {
                shorthand = "";
                return false;
            }
            shorthand = String.Concat(":", key, ":");
            return true;

        }
    }
}
