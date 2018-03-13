using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace KiteBotCore.Utils
{
    public class KiteTimeSpanReader : TypeReader
    {
        private static readonly Regex Reg = new Regex(@"^(?<weeks>\d+w)?(?<days>\d+d)?(?<hours>\d{1,2}h)?(?<minutes>\d{1,2}m)?(?<seconds>\d{1,2}s)?$", RegexOptions.Compiled);
        public override async Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider services)
        {
            await Task.Yield();

            var result = TimeSpan.Zero;
            if (input == "0")
                return TypeReaderResult.FromSuccess((TimeSpan?)null);

            if (TimeSpan.TryParse(input, out result))
                return TypeReaderResult.FromSuccess(result);

            
            var gps = new[] { "weeks", "days", "hours", "minutes", "seconds" };
            var mtc = Reg.Match(input);
            if (!mtc.Success)
                return TypeReaderResult.FromError(CommandError.ParseFailed, "Invalid TimeSpan string");

            int w = 0;
            int d = 0;
            int h = 0;
            int m = 0;
            int s = 0;
            foreach (var gp in gps)
            {
                var gpc = mtc.Groups[gp].Value;
                if (string.IsNullOrWhiteSpace(gpc))
                    continue;

                var gpt = gpc.Last();
                int.TryParse(gpc.Substring(0, gpc.Length - 1), out var val);
                switch (gpt)
                {
                    case 'w':
                        w = val;
                        break;
                    case 'd':
                        d = val;
                        break;
                    case 'h':
                        h = val;
                        break;
                    case 'm':
                        m = val;
                        break;
                    case 's':
                        s = val;
                        break;
                }
            }
            result = new TimeSpan((w*7) + d, h, m, s);
            return TypeReaderResult.FromSuccess(result);
        }
    }
}