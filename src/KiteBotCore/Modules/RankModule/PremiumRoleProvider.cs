using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Json;

namespace KiteBotCore.Modules.RankModule
{
    public class PremiumRoleProvider : IRoleProvider
    {
        private readonly RankConfigs _config;

        public PremiumRoleProvider(RankConfigs config)
        {
            _config = config;
        }

        public async Task<List<IRankRole>> GetUserRoles(IGuildUser user, KiteBotDbContext dbContext)
        {
            var dbUser = await dbContext.Users.FindAsync((long) user.Id).ConfigureAwait(false);
            if (dbUser.Premium)
            {
                var premium = _config.GuildConfigs[user.Guild.Id].Premium ?? null;
                if(premium != null)
                    return new List<IRankRole>{premium};
            }
            return new List<IRankRole>(0);
            
        }

        public List<IRankRole> GetAllRanks(IGuild guild)
        {
            var premium = _config.GuildConfigs[guild.Id].Premium ?? null;
            if (premium != null)
            {
                return new List<IRankRole> {_config.GuildConfigs[guild.Id].Premium};
            }
            return new List<IRankRole>(0);
        }

        public async Task<List<IColor>> GetUserColors(IGuildUser user, KiteBotDbContext dbContext)
        {
            var dbUser = await dbContext.Users.FindAsync((long)user.Id).ConfigureAwait(false);
            if (dbUser.Premium)
            {
                var premium = _config.GuildConfigs[user.Guild.Id].Premium ?? null;
                if (premium != null)
                    return new List<IColor>(_config.GuildConfigs[user.Guild.Id].Premium.Colors);
            }
            return new List<IColor>(0);
        }

        public List<IColor> GetAllColors(IGuild guild, KiteBotDbContext dbContext)
        {
            var premium = _config.GuildConfigs[guild.Id].Premium ?? null;
            if (premium != null)
            {
                return new List<IColor>(_config.GuildConfigs[guild.Id].Premium.Colors);
            }
            return new List<IColor>(0);
        }

        public async Task<List<IColor>> GetAllColorsAsync(IGuild guild, KiteBotDbContext dbContext)
        {
            await Task.Delay(0);
            return GetAllColors(guild, dbContext);
        }
    }
}