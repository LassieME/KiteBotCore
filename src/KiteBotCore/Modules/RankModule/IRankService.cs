using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace KiteBotCore.Modules.RankModule
{
    public interface IRankService
    {
        Task<List<IRankRole>> GetAwardedRolesAsync(IGuildUser user, IGuild guild, DateTimeOffset joinDate = default,
            DateTimeOffset lastActivity = default);

        Task<DateTimeOffset> GetUserLastActivityAsync(IGuildUser inputUser, IGuild guild);

        Task<DateTimeOffset> GetUserJoinDateAsync(IUser inputUser, IGuild guild);

        Task<IEnumerable<IColor>> GetAvailableColorsAsync(IGuildUser user, IGuild guild);

        /// <summary>
        /// May return null if there is no next rank available
        /// </summary>
        /// <param name="user"></param>
        /// <param name="guild"></param>
        /// <returns></returns>
        Task<(Discord.IRole role, TimeSpan timeToRole)> GetNextRoleAsync(IGuildUser user, IGuild guild);

        void AddRank(IGuild guild, Discord.IRole role, TimeSpan timeSpan);

        bool RemoveRank(IGuild guild, Discord.IRole role);

        IEnumerable<IRankRole> GetRanksForGuild(IGuild guild);

        void AddColorToRank(IGuild guild, Discord.IRole role, Discord.IRole colorRole);

        bool RemoveColorFromRank(IGuild guild, Discord.IRole rankRole, Discord.IRole colorRole);

        void EnableGuildRanks(IGuild guild);

        Task SetJoinDateAsync(IUser user, IGuild guild, DateTimeOffset newDate);

        Task<bool> AssignColorToUserAsync(IGuild guild, IUser userId, Discord.IRole colorRole, TimeSpan? timeToRemove);

        Task<bool> UnassignColorFromUserAsync(IUser user, Discord.IRole rankRole);

        Task<IList<(ulong roleId, DateTimeOffset? expiry)>> GetAssignedRolesAsync(IGuild guildId, IUser userId);

        Task FlushQueueAsync();
    }
}