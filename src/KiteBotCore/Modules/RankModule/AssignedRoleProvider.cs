using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace KiteBotCore.Modules.RankModule
{
    public class AssignedRoleProvider : IRoleProvider
    {
        public async Task<List<IRankRole>> GetUserRoles(IGuildUser user, KiteBotDbContext dbContext)
        {
            await Task.Delay(0);
            return new List<IRankRole>();
        }

        public List<IRankRole> GetAllRanks(IGuild guild)
        {
            return new List<IRankRole>();
        }

        public async Task<List<IColor>> GetUserColors(IGuildUser user, KiteBotDbContext dbContext)
        {
            long id = (long)user.Id;
            //var dbUser = await dbContext.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(x => x.UserId == id);
            var dbUser = await dbContext.Users.FindAsync(id).ConfigureAwait(false);
            if (dbUser.UserRoles != null)
            {
                return new List<IColor>(dbUser.UserRoles);
            }
            else
            {
                return new List<IColor>(0);
            }
        }

        public List<IColor> GetAllColors(IGuild guild, KiteBotDbContext dbContext)
        {
            return GetAllColorsAsync(guild, dbContext).GetAwaiter().GetResult();
        }

        public async Task<List<IColor>> GetAllColorsAsync(IGuild guild, KiteBotDbContext dbContext)
        {
            await dbContext.UserColorRoles.LoadAsync().ConfigureAwait(false);
            return new List<IColor>(dbContext.UserColorRoles.DistinctBy(x => x.RoleId));
        }
    }

    public static class LinqExtensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}