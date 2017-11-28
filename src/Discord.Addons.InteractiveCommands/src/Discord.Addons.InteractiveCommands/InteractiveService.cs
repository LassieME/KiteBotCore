using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Addons.InteractiveCommands
{
    public class InteractiveService
    {
        private readonly DiscordSocketClient _client;

        public InteractiveService(DiscordSocketClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Waits for a message to be sent by the user.
        /// </summary>
        /// <param name="user">The user to await a message from.</param>
        /// <param name="channel">Optional channel that this message must be sent in.</param>
        /// <param name="timeout">How long to wait for a message? Use TimeSpan.Zero for infinite timeout.</param>
        /// <param name="preconditions">A list of ResponsePreconditions to constrain the response to.</param>
        /// <returns>The message that the user sends.</returns>
        public async Task<IUserMessage> WaitForMessage(IUser user, IMessageChannel channel = null, TimeSpan? timeout = null, params ResponsePrecondition[] preconditions)
        {
            if (timeout == null) timeout = TimeSpan.FromSeconds(15);

            var blockToken = new CancellationTokenSource();
            IUserMessage response = null;

            Func<IMessage, Task> isValid = async (messageParameter) =>
            {
                var message = messageParameter as IUserMessage;
                if (message == null) return;
                if (message.Author.Id != user.Id) return;
                if (channel != null && message.Channel.Id != channel.Id) return;

                var context = new ResponseContext(_client, message);

                foreach (var precondition in preconditions)
                {
                    var result = await precondition.CheckPermissions(context);
                    if (!result.IsSuccess) return;
                }

                response = message;
                blockToken.Cancel(true);
            };

            _client.MessageReceived += isValid;
            try
            {
                if (timeout == TimeSpan.Zero)
                    await Task.Delay(-1, blockToken.Token);
                else
                    await Task.Delay(timeout.Value, blockToken.Token);
            }
            catch (TaskCanceledException)
            {
                return response;
            }
            catch
            {
                throw;
            }
            finally
            {
                _client.MessageReceived -= isValid;
            }
            return null; // this should never happen
        }
    }
}
