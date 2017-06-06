using Discord;
using Discord.Commands;
using Serilog;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KiteBotCore.Modules
{
    public class MarkovChain : CleansingModuleBase
    {
        private Stopwatch _stopwatch;
        protected override void BeforeExecute()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute()
        {
            _stopwatch.Stop();
            Log.Debug($"Markov Chain Command: {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }

        [Command("testMarkov", RunMode = RunMode.Async)]
        [Alias("tm")]
        [Summary("Creates a Markov Chain string based on user messages")]
        [RequireServer(Server.KiteCo)]
        public async Task MarkovChainCommand(string haiku = null)
        {
            if (haiku == "haiku")
            {
                await ReplyAsync(KiteChat.MultiDeepMarkovChains.GetSequence() + "\n" +
                                 KiteChat.MultiDeepMarkovChains.GetSequence() + "\n" +
                                 KiteChat.MultiDeepMarkovChains.GetSequence() + "\n").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync(KiteChat.MultiDeepMarkovChains.GetSequence()).ConfigureAwait(false);
            }
        }

        [Command("feed", RunMode = RunMode.Async)]
        [Summary("Downloads and feeds the markovchain")]
        [RequireBotOwner, RequireServer(Server.KiteCo)]
        public async Task FeedCommand(int amount)
        {
            var messagesTask = Context.Channel.GetMessagesAsync(amount).Flatten().ConfigureAwait(false);
            int i = 0;
            List<Message> list = await KiteChat.MultiDeepMarkovChains.GetFullDatabase(Context.Channel.Id).ConfigureAwait(false);
            var messages = await messagesTask;
            foreach(var msg in messages)
            {
                i++;
                if (list.All(x => x.Id != msg.Id))
                {
                    await KiteChat.MultiDeepMarkovChains.Feed(msg).ConfigureAwait(false);
                }
            }

            await KiteChat.MultiDeepMarkovChains.SaveAsync().ConfigureAwait(false);
            await ReplyAsync($"{i} messages downloaded.").ConfigureAwait(false);
        }


        [Command("setdepth", RunMode = RunMode.Async)]
        [Summary("Downloads and feeds the markovchain")]
        [RequireBotOwner, RequireServer(Server.KiteCo)]
        public async Task SetDepthCommand(int depth)
        {
            KiteChat.MultiDeepMarkovChains.SetDepth(depth);
            
            await ReplyAsync("👌").ConfigureAwait(false);
        }

        [Command("remove", RunMode = RunMode.Async)]
        [Summary("Removes a message from the remote database by messageId")]
        [RequireBotOwner, RequireServer(Server.KiteCo)]
        public async Task RemoveCommand(ulong messageId)
        {
            List<Message> list = await KiteChat.MultiDeepMarkovChains.GetFullDatabase(0).ConfigureAwait(false);

            foreach (var item in list.Where(x => x.Id == messageId))
                await KiteChat.MultiDeepMarkovChains.RemoveItemAsync(item).ConfigureAwait(false);

            await ReplyAsync("<:bop:230275292076179460>").ConfigureAwait(false);
        }
    }
}
