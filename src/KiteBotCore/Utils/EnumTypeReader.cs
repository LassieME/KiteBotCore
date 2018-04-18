using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace KiteBotCore.Utils
{
    public class EnumTypeReader<T> : TypeReader where T : struct, IComparable, IConvertible, IFormattable
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            await Task.Yield();
            var success = Enum.TryParse(input, true, out T value);

            return success ? TypeReaderResult.FromSuccess(value) : TypeReaderResult.FromError(CommandError.ParseFailed, "Enum parsing failed");
        }
    }
}