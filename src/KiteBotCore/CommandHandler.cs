using System;
using System.Threading.Tasks;
using System.Reflection;
using Discord.Commands;
using Discord.WebSocket;

namespace KiteBotCore
{
    public class CommandHandler
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IDependencyMap _map;

        public async Task Install(IDependencyMap map)
        {
            // Create Command Service, inject it into Dependency Map
            _client = map.Get<DiscordSocketClient>();
            _commands = new CommandService();
            map.Add(_commands);
            _map = map;

            await _commands.AddModules(Assembly.GetEntryAssembly());
            
            _client.MessageReceived += HandleCommand;
        }

        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null) return;

            // Mark where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message has a valid prefix, adjust argPos 
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos))) return;

            // Create a Command Context
            try
            {
                var context = new CommandContext(_client, message);

                // Execute the Command, store the result
                var result = await _commands.Execute(context, argPos, _map);

                // If the command failed, notify the user
                if (!result.IsSuccess)
                    await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + ": " + ex.Message);
            }
        }
    }
}