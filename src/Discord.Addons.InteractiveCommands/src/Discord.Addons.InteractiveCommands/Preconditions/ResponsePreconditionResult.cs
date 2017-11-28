using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using System.Diagnostics;

namespace Discord.Addons.InteractiveCommands
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public struct ResponsePreconditionResult : IResult
    {
        public CommandError? Error { get; }
        public string ErrorReason { get; }

        public bool IsSuccess => !Error.HasValue;

        private ResponsePreconditionResult(CommandError? error, string errorReason)
        {
            Error = error;
            ErrorReason = errorReason;
        }

        public static ResponsePreconditionResult FromSuccess()
            => new ResponsePreconditionResult(null, null);
        public static ResponsePreconditionResult FromError(string reason)
            => new ResponsePreconditionResult(CommandError.UnmetPrecondition, reason);
        public static ResponsePreconditionResult FromError(IResult result)
            => new ResponsePreconditionResult(result.Error, result.ErrorReason);

        public override string ToString() => IsSuccess ? "Success" : $"{Error}: {ErrorReason}";
        private string DebuggerDisplay => IsSuccess ? "Success" : $"{Error}: {ErrorReason}";

    }
}
