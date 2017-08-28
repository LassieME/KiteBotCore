using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace KiteBotCore.Modules
{
    /// <summary> Sets how often a user is allowed to use this command. </summary>
    /// <remarks>This is backed by an in-memory collection
    /// and will not persist with restarts.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireDateSpanAttribute : PreconditionAttribute
    {
        private readonly DateTimeOffset _fromDateTimeOffset;
        private readonly DateTimeOffset _toDateTimeOffset;

        public RequireDateSpanAttribute(string fromDateTime, string toDateTime)
        {
            _fromDateTimeOffset = DateTimeOffset.ParseExact(fromDateTime, @"dd/MM/yyyy HH:mm:ss", new CultureInfo("no"));
            _toDateTimeOffset = DateTimeOffset.ParseExact(toDateTime, @"dd/MM/yyyy HH:mm:ss", new CultureInfo("no"));
        }

        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (DateTimeOffset.UtcNow.CompareTo(_fromDateTimeOffset) > 0 && DateTimeOffset.UtcNow.CompareTo(_toDateTimeOffset) < 0 )
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            return Task.FromResult(PreconditionResult.FromError($"You can only use this command between {_fromDateTimeOffset.ToString()} and {_toDateTimeOffset.ToString()}"));
        }
    }
}