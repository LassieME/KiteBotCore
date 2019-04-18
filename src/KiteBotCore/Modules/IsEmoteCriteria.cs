using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace KiteBotCore.Modules
{
    class IsEmoteCriteria : ICriterion<SocketReaction>
    {
        public string Emote;
        public ShineSBet Bet;

        public IsEmoteCriteria(ShineSBet bet, string emote)
        {
            Bet = bet;
            Emote = emote;
        }

        public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketReaction parameter)
        {
            return Task.FromResult(parameter.Emote.Name == Emote);            
        }
    }
}
