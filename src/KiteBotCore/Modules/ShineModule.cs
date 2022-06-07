using Discord;
using Discord.Addons.Interactive;
using Discord.Addons.EmojiTools;
using Discord.Commands;
using Discord.WebSocket;
using KiteBotCore.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KiteBotCore.Modules
{
    //[RequireContext(ContextType.Guild), RequireServer(Server.GiantBomb)]
    public class ShineModule : ModuleBase
    {
        public MyInteractiveService MyInteractiveService { get; set;}
        public ShineService ShineService { get; set; }


        private Stopwatch _stopwatch;
        protected override void BeforeExecute(CommandInfo command)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute(CommandInfo command)
        {
            _stopwatch.Stop();
            Log.Debug($"Shine Command ({command.Aliases[0]}): {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }

        [Command("test"), RequireBotOwner, RequireChannel(213359477162770433)]
        [Summary("Creates a shine bet, in the format [Shines] [Bet question] [Time before closing] ")]
        public async Task BetRoll(int shines, string title, TimeSpan timeSpan)
        {
            var createdBet = await ShineService.CreateBetAsync((SocketCommandContext)Context, shines, timeSpan, title);
            var message = await ReplyAsync("", false, (new EmbedBuilder() { Title = $"#{createdBet.Id}: {title}" }).Build());//Replace with posting message to a channel
            
            MyInteractiveService.AddReactionCallback(message, new ShineCallback(createdBet, (SocketCommandContext)Context, "👍", timeSpan, ShineService.AddBetPositive));
            MyInteractiveService.AddReactionCallback(message, new ShineCallback(createdBet, (SocketCommandContext)Context, "👎", timeSpan, ShineService.AddBetNegative));
            //InteractiveService.AddReactionCallback(message, new ShineCallback(createdBet, Context, "2⃣", timeSpan, ShineService.AddDoubleDown));  
            await message.AddReactionsAsync(new Emoji[] { EmojiExtensions.FromText("thumbsup"), EmojiExtensions.FromText("thumbsdown") });
            _ = Task.Run(async () => { await Task.Delay(timeSpan); await message.RemoveAllReactionsAsync(); });
        }

        [Command("test2"), RequireBotOwner]
        [Summary("Creates a shine bet, in the format [Shines] [Bet question] [Time before closing] ")]
        public async Task BetsRoll(int shines, string title, TimeSpan timeSpan)
        {
            
            var message = await ReplyAsync("", false, (new EmbedBuilder()
            {
                Title = $"**#{0001}**: Is this the run? 2 Shines",
                Footer = new EmbedFooterBuilder()
                {
                    Text = $"Betting is open until"
                },
                Timestamp = DateTimeOffset.UtcNow + timeSpan
            }).Build());

            await message.AddReactionsAsync(new Emoji[] { EmojiExtensions.FromText("thumbsup"), EmojiExtensions.FromText("thumbsdown") });
        }
    }
    
    public class ShineCallback : IReactionCallback
    {
        public RunMode RunMode => RunMode.Async;

        public ShineSBet BetId { get; }

        public ICriterion<SocketReaction> Criterion { get; }

        public TimeSpan? Timeout { get; }

        public SocketCommandContext Context { get; private set; }

        public string Emote { get; private set; }

        public Func<int, SocketReaction, Task<bool>> Action { get; private set; }

        public ShineCallback(ShineSBet betId, SocketCommandContext context, string emote, TimeSpan timeout, Func<int, SocketReaction, Task<bool>> action)
        {
            BetId = betId;
            Context = context;
            Emote = emote;
            Timeout = timeout;
            Criterion = new IsEmoteCriteria(BetId, emote);
            Action = action;
        }        

        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            return await Action(BetId.Id, reaction);
        }
    }
}
