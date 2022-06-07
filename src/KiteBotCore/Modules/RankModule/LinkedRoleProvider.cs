using Discord;
using KiteBotCore.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KiteBotCore.Modules.RankModule
{
    class LinkedRoleProvider : IRoleProvider
    {
        private RankConfigs rankConfigs;

        public LinkedRoleProvider(RankConfigs rankConfigs)
        {
            this.rankConfigs = rankConfigs;
        }

        public List<IColor> GetAllColors(IGuild guild, KiteBotDbContext dbContext)
        {
            if (rankConfigs.GuildConfigs.TryGetValue(guild.Id, out var guildConfig))
            {
                var ColorList = new List<IColor>();
                foreach (var x in guildConfig.LinkedColors.Values) 
                {
                    ColorList.AddRange(x);
                }
                return new List<IColor>(ColorList.DistinctBy(x => x.Id));
            }
            return new List<IColor>();
        }

        public Task<List<IColor>> GetAllColorsAsync(IGuild guild, KiteBotDbContext dbContext)
        {
            return Task.FromResult(GetAllColors(guild, dbContext));
        }

        public List<IRankRole> GetAllRanks(IGuild guild)
        {
            return new List<IRankRole>();
        }

        public Task<List<IColor>> GetUserColors(IGuildUser user, KiteBotDbContext dbContext)
        {
            var ColorList = new List<IColor>();
            if (rankConfigs.GuildConfigs.TryGetValue(user.GuildId, out var guildConfig))
            {
                foreach (var roleId in user.RoleIds) 
                {
                    if (guildConfig.LinkedColors.TryGetValue(roleId, out List<Json.Color> list)) 
                    {
                        ColorList.AddRange(list);
                    }
                }                
                return Task.FromResult(new List<IColor>(ColorList.DistinctBy(x => x.Id)));
            }
            return Task.FromResult(ColorList);
        }

        public Task<List<IRankRole>> GetUserRoles(IGuildUser user, KiteBotDbContext dbContext)
        {
            return Task.FromResult(new List<IRankRole>());
        }
    }
}
