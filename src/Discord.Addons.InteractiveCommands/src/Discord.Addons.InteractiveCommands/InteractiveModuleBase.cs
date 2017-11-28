using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading;

namespace Discord.Addons.InteractiveCommands
{
    public abstract class InteractiveModuleBase : InteractiveModuleBase<CommandContext> { }
    public abstract class InteractiveModuleBase<T> : ModuleBase<T> where T : class, ICommandContext
    {
        /// <summary>
        /// Waits for the user to send a message.
        /// </summary>
        /// <param name="user">Which user to wait for a message from.</param>
        /// <param name="channel">Which channel the message should be sent in. (If null, will accept a response from any channel).</param>
        /// <param name="timeout">How long to wait for a message before timing out. This value will default to 15 seconds.</param>
        /// <param name="preconditions">Any preconditions to run to determine if a response is valid.</param>
        /// <returns>The response.</returns>
        /// <remarks>When you use this in a command, the command's RunMode MUST be set to 'async'. Otherwise, the gateway thread will be blocked, and this will never return.</remarks>
        public async Task<IUserMessage> WaitForMessage(IUser user, IMessageChannel channel = null, TimeSpan? timeout = null, params ResponsePrecondition[] preconditions)
        {
            var client = Context.Client as DiscordSocketClient;
            if (client == null) throw new NotSupportedException("This addon must be ran with a DiscordSocketClient.");
            return await new InteractiveService(client).WaitForMessage(user, channel, timeout, preconditions);
        }

        protected virtual Task<IUserMessage> ReplyAsync(string message, bool isTTS = false, EmbedBuilder embed = null, uint deleteAfter = 0, RequestOptions options = null)
        {
            return Context.Channel.SendMessageAsync(message, isTTS, embed, deleteAfter, options);
        }
    }
}
