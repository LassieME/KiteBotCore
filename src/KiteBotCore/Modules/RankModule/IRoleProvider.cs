using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Json;

namespace KiteBotCore.Modules.RankModule
{
    public interface IRoleProvider
    {
        Task<List<IRankRole>> GetUserRoles(IGuildUser user, KiteBotDbContext dbContext);

        List<IRankRole> GetAllRanks(IGuild guild);

        Task<List<IColor>> GetUserColors(IGuildUser user, KiteBotDbContext dbContext);

        List<IColor> GetAllColors(IGuild guild, KiteBotDbContext dbContext);

        Task<List<IColor>> GetAllColorsAsync(IGuild guild, KiteBotDbContext dbContext);
    }
}