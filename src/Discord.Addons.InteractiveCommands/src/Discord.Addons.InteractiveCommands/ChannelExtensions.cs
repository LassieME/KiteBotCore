using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Addons.InteractiveCommands
{
    public static class ChannelExtensions
    {
        // thanks discord.py
        /// <inheritdoc />
        /// <param name="deleteAfter">The time (in seconds) to wait before deleting the message.</param>
        public static async Task<IUserMessage> SendMessageAsync(this IMessageChannel channel, 
            string text,
            bool isTTS = false,
            EmbedBuilder embed = null,
            uint deleteAfter = 0,
            RequestOptions options = null)
        {
            var message = await channel.SendMessageAsync(text, isTTS, embed, options);
            if (deleteAfter > 0)
            {
                var _ = Task.Run(() => DeleteAfterAsync(message, deleteAfter));
            }
            return message;
        }
        private static async Task DeleteAfterAsync(IUserMessage message, uint deleteAfter)
        {
            await Task.Delay(TimeSpan.FromSeconds(deleteAfter));
            await message.DeleteAsync();
        }
    }
}
