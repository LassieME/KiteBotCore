using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;
using System.Threading.Tasks;

namespace KiteBotCore.Modules.Rank
{
    class Rank : ModuleBase
    {
        private RankService _rankService;

        public Rank(RankService rankService)
        {
            _rankService = rankService;
        }

        [Command("rank")]
        [Summary("Shows you your current rank, based on the amount of time since your first tracked GuildJoin")]
        public async Task RankCommand()
        {
            var result = await _rankService.CheckUserJoinDate(Context.User, Context.Guild);
        }
    }
}
