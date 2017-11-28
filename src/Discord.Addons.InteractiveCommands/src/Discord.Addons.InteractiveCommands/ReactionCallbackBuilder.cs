using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Addons.InteractiveCommands
{
    public class ReactionCallbackBuilder
    {
        public IDiscordClient Client { get; set; }

        public string[] Reactions { get; set; } = Array.Empty<string>();

        public Func<IUser, Task<bool>> Precondition { get; set; }

        public int Timeout { get; set; } = 60000;

        public Dictionary<string, ReactionCallback> Callbacks { get; set; } = new Dictionary<string, ReactionCallback>();

        /// <summary>
        /// Sets the Discord client.
        /// </summary>
        /// <param name="client">Must be either a DiscordSocketClient or DiscordShardedClient.</param>
        public ReactionCallbackBuilder WithClient(IDiscordClient client)
        {
            Client = client;
            return this;
        }

        /// <summary>
        /// Sets reactions which will be added to the message.
        /// </summary>
        public ReactionCallbackBuilder WithReactions(params string[] reactions)
        {
            Reactions = reactions;
            return this;
        }

        /// <summary>
        /// Sets a preconditon on which the callbacks will be executed.
        /// </summary>
        public ReactionCallbackBuilder WithPrecondition(Func<IUser, Task<bool>> func)
        {
            Precondition = func;
            return this;
        }

        public ReactionCallbackBuilder WithTimeout(int ms)
        {
            Timeout = ms;
            return this;
        }

        /// <summary>
        /// Sets a preconditon on which the callbacks will be executed.
        /// </summary>
        public ReactionCallbackBuilder WithPrecondition(Func<IUser, bool> func)
        {
            Precondition = x => Task.FromResult(func(x));
            return this;
        }

        /// <summary>
        /// Adds a callback to the builder.
        /// </summary>
        /// <param name="emoji">The emoji on which the callback will be executed.</param>
        /// <param name="callback">The callback which will be executed when the user adds a reaction with the specified emoji.</param>
        /// <param name="resumeAfterExecution">If it should execute further callbacks after this callback has been executed.</param>
        public ReactionCallbackBuilder AddCallback(string emoji, Func<IUser, Task> callback, bool resumeAfterExecution = false)
        {
            Callbacks.Add(emoji, new ReactionCallback { Function = callback, ResumeAfterExecution = resumeAfterExecution });
            return this;
        }

        /// <summary>
        /// Adds a callback to the builder.
        /// </summary>
        /// <param name="emoji">The emoji on which the callback will be executed.</param>
        /// <param name="callback">The callback which will be executed when the user adds a reaction with the specified emoji.</param>
        /// <param name="resumeAfterExecution">If it should execute further callbacks after this callback has been executed.</param>
        public ReactionCallbackBuilder AddCallback(string emoji, Action<IUser> callback, bool resumeAfterExecution = false)
        {
            Callbacks.Add(emoji, new ReactionCallback { Function = x => { callback(x); return Task.CompletedTask; }, ResumeAfterExecution = resumeAfterExecution });
            return this;
        }

        /// <summary>
        /// Executes the builder.
        /// </summary>
        /// <param name="message">The message to executes the callbacks on.</param>
        public Task ExecuteAsync(IUserMessage message)
        {
            if (Client == null)
                throw new InvalidOperationException("Client is not specified.");
            if (Client is DiscordSocketClient socketClient)
                return ExecuteAsync(socketClient, message);
            if (Client is DiscordShardedClient shardedClient)
                return ExecuteAsync(shardedClient, message);
            throw new InvalidOperationException("Client must be a either a DiscordSocketClient or DiscordShardedClient.");
        }

        private Task ExecuteAsync(DiscordShardedClient client, IUserMessage message)
        {
            DiscordSocketClient socketClient;
            if (message.Channel is IGuildChannel guildChannel)
                socketClient = client.GetShardFor(guildChannel.Guild);
            else socketClient = client.GetShard(0); //shard for DMs
            return ExecuteAsync(socketClient, message);
        }

        private async Task ExecuteAsync(DiscordSocketClient client, IUserMessage message)
        {
            var tokenSource = new CancellationTokenSource();
            var timeoutDate = DateTime.UtcNow.AddMilliseconds(Timeout);
            bool isFinished = false;
            Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> func = async (msg, channel, reaction) =>
            {
                if (msg.Id != message.Id || !reaction.User.IsSpecified || reaction.UserId == client.CurrentUser.Id)
                    return;
                var emoji = reaction.Emote;
                string emojiString = emoji is Emote emote ? $"{emote.Name}:{emote.Id}" : emoji.Name;
                var user = reaction.User.Value;
                if (!Callbacks.TryGetValue(emojiString, out var callback))
                    return;
                if (Precondition != null && !await Precondition(user))
                    return;
                timeoutDate = DateTime.UtcNow.AddMilliseconds(Timeout);
                try
                {
                    await callback.Function(reaction.User.Value);
                }
                catch
                {
                    tokenSource.Cancel(true);
                    return;
                }
                if (callback.ResumeAfterExecution)
                    await message.RemoveReactionAsync(emoji, user);
                else
                    tokenSource.Cancel(true);
            };
            client.ReactionAdded += func;
            foreach (string emoji in Reactions)
                await message.AddReactionAsync(new Emoji(emoji));
            do
            {
                try
                {
                    var timeout = timeoutDate - DateTime.UtcNow;
                    if (timeout.Ticks >= 0)
                        await Task.Delay(timeout, tokenSource.Token);
                    if (DateTime.UtcNow >= timeoutDate)
                        isFinished = true;
                }
                catch (TaskCanceledException)
                {
                    isFinished = true;
                }
            } while (!isFinished);
            client.ReactionAdded -= func;
            try
            {
                await message.RemoveAllReactionsAsync();
            }
            catch { }
        }
    }
}
