using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Json;

namespace KiteBotCore.Modules.RankModule
{
    public class RankRoleProvider : IRoleProvider
    {
        private readonly RankConfigs _configs;

        public RankRoleProvider(RankConfigs configs)
        {
            _configs = configs;
        }

        public async Task<List<IRankRole>> GetUserRoles(IGuildUser user, KiteBotDbContext dbContext)
        {
            var dbUser = await dbContext.Users.FindAsync((long) user.Id).ConfigureAwait(false);

            var joinDate = dbUser.JoinedAt;
            var lastActivity = dbUser.LastActivityAt;
                
            var timeInGuild = DateTimeOffset.UtcNow - joinDate;
            var allRanksInGuild = GetAllRanks(user.Guild);

            return allRanksInGuild
                .Where(x => x.RequiredTimeSpan < timeInGuild)
                .Take(allRanksInGuild
                          .Where(x => x.RequiredTimeSpan < timeInGuild).ToList()
                          .Count - (DateTimeOffset.UtcNow - lastActivity).Days / 7).ToList();
        }

        public List<IRankRole> GetAllRanks(IGuild guild)
        {
            return new List<IRankRole>(_configs.GuildConfigs[guild.Id].Ranks.Values);
        }

        public async Task<List<IColor>> GetUserColors(IGuildUser user, KiteBotDbContext dbContext)
        {
            return new List<IColor>((await GetUserRoles(user, dbContext).ConfigureAwait(false)).SelectMany(x => x.Colors));
        }

        public List<IColor> GetAllColors(IGuild guild, KiteBotDbContext dbContext)
        {
            return new List<IColor>(_configs.GuildConfigs[guild.Id].Ranks.Values.SelectMany(x => x.Colors));
        }

        public async Task<List<IColor>> GetAllColorsAsync(IGuild guild, KiteBotDbContext dbContext)
        {
            await Task.Delay(0);
            return GetAllColors(guild, dbContext);
        }
    }
}