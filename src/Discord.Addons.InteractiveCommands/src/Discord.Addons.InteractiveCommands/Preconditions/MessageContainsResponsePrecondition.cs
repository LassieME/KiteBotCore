using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Addons.InteractiveCommands
{
    public class MessageContainsResponsePrecondition : ResponsePrecondition
    {
        private readonly string[] validKeywords;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageContainsResponsePrecondition"/> class.
        /// </summary>
        /// <param name="keywords">The keywords the response must contain.</param>
        public MessageContainsResponsePrecondition(params string[] keywords)
        {
            validKeywords = keywords;
        }

        public override Task<ResponsePreconditionResult> CheckPermissions(ResponseContext context)
        {
            if (!validKeywords.Any(s => context.Response.Content.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)) return Task.FromResult(ResponsePreconditionResult.FromError("Response did not contain a valid keyword."));
            return Task.FromResult(ResponsePreconditionResult.FromSuccess());
        }
    }
}
