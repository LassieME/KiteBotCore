using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace KiteBotCore.Modules
{
    public class ShineService
    {
        private DiscordContextFactory _DbFactory { get; }

        public ConcurrentDictionary<int, ShineSBet> BetsDict = new ConcurrentDictionary<int, ShineSBet>();

        public ShineService(DiscordContextFactory _dbFactory)
        {
            _DbFactory = _dbFactory;
        }        

        public async Task<ShineSBet> CreateBetAsync(SocketCommandContext context, int shines, TimeSpan timeSpan, string question)
        {
            using (var db = _DbFactory.Create())
            {
                var user = await db.FindAsync<User>((long)context.User.Id);
                var channel = await db.FindAsync<Channel>((long)context.Channel.Id);

                if (user.Shines >= shines)
                {
                    var shineBetEvent = new ShineBetEvent()
                    {
                        CreationDateTime = DateTimeOffset.UtcNow,
                        TimeUntilClose = timeSpan,
                        Question = question,
                        BetAmount = shines,
                        OwnerUser = user,
                        Channel = channel,
                        ShineBets = new List<ShineBet>()
                    };
                    db.Add(shineBetEvent);                    
                    var bet = new ShineBet() { Answer = true, User = user, ShineBetEvent = shineBetEvent};
                    db.Add(bet);
                    shineBetEvent.ShineBets.Add(bet);
                    user.Shines -= shineBetEvent.BetAmount;
                    await db.SaveChangesAsync();
                    var id = shineBetEvent.ShineBetEventId;
                    var shineS = new ShineSBet(id, DateTimeOffset.UtcNow + timeSpan);
                    BetsDict.TryAdd(id, shineS);
                    return shineS;
                }
                else
                {
                    throw new Exception("You dont have enough shines to start this bet");
                }
            }
        }

        public Task<bool> AddBetPositive(int betId, SocketReaction reaction)
        {
            return AddBet(betId, reaction, true);
        }

        public Task<bool> AddBetNegative(int betId, SocketReaction reaction)
        {
            return AddBet(betId, reaction, false);
        }

        public async Task<bool> AddBet(int betId, SocketReaction reaction, bool TrueOrFalse)
        {
            if (BetsDict.TryGetValue(betId, out ShineSBet b) && b.DateTimeOffset > DateTimeOffset.UtcNow)
            {
                var userBet = b.dict.GetOrAdd(reaction.UserId, (TrueOrFalse, false));
                if (userBet.bet == !TrueOrFalse) { return false; } //already voted other
                using (var db = _DbFactory.Create())
                {
                    var user = await db.FindAsync<User>((long)reaction.UserId);
                    var shineBetEvent = await db.FindAsync<ShineBetEvent>(betId);
                    if (shineBetEvent.BetAmount <= user.Shines)
                    {
                        var bet = new ShineBet() { Answer = TrueOrFalse, User = user, ShineBetEvent = shineBetEvent };
                        db.Add(bet);
                        shineBetEvent.ShineBets.Add(bet);
                        await db.SaveChangesAsync();
                        user.Shines -= shineBetEvent.BetAmount;
                        return true;
                    }
                    else
                    {
                        b.dict.Remove(reaction.UserId, out _);
                        return false;
                    }
                }
            }
            else
            {
                //Bet does not exist
                return false;
            }
        }

        public async Task<bool> AddDoubleDown(int betId, SocketReaction reaction)
        {
            if (BetsDict.TryGetValue(betId, out ShineSBet b))
            {
                if (b.DateTimeOffset > DateTimeOffset.UtcNow && b.dict.TryGetValue(reaction.UserId, out (bool? bet, bool doubleDown) userBet))
                {
                    if (userBet.doubleDown == true) return false;
                    using (var db = _DbFactory.Create())
                    {
                        var user = await db.FindAsync<User>((long)reaction.UserId);                        
                        var shineBetEvent = await db.FindAsync<ShineBetEvent>(betId);

                        if (user.Shines < shineBetEvent.BetAmount) return false;
                        
                        var shineBet = shineBetEvent.ShineBets.Find(x => x.User.UserId == (long)reaction.UserId);
                        shineBet.DoubleDown = true;                        
                        user.Shines -= shineBetEvent.BetAmount;
                        await db.SaveChangesAsync();
                        userBet.doubleDown = true;
                        return true;
                        
                    }
                }
                else
                {
                    //Yes/No vote does not exist or is over
                    return false;
                }
            }
            else
            {
                //Bet does not exist
                return false;
            }
        }
    }

    public class ShineSBet
    {
        public int Id;
        public DateTimeOffset DateTimeOffset;
        public ConcurrentDictionary<ulong, (bool? bet, bool doubleDown)> dict = new ConcurrentDictionary<ulong, (bool? bet, bool doubleDown)>();

        public ShineSBet(int id, DateTimeOffset dateTimeOffset)
        {
            Id = id;
            DateTimeOffset = dateTimeOffset;
        }        
    }
}