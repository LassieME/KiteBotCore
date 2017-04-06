using System;
using System.Threading.Tasks;
using System.Reflection;
using Discord.Commands;
using Discord.WebSocket;
using KiteBotCore.Json;
using Serilog;

namespace KiteBotCore
{
    public class CommandHandler
    {
        public CommandService Commands;
        private DiscordSocketClient _client;
        private KiteChat _kiteChat;
        private IDependencyMap _map;
        private char _prefix;
        private ulong _ownerId;

        public async Task InstallAsync(IDependencyMap map)
        {
            _map = map;
            _kiteChat = map.Get<KiteChat>();
            Commands = new CommandService();
            _client = map.Get<DiscordSocketClient>();

            if (map.TryGet(out BotSettings botSettings))
            {
                _ownerId = botSettings.OwnerId;
                _prefix = botSettings.CommandPrefix;
            }

            await Commands.AddModulesAsync(Assembly.GetEntryAssembly()).ConfigureAwait(false);
            if (_client.CurrentUser.IsBot)
            {
                _client.MessageReceived += HandleBotCommand;
            }
            else
            {
                //Override _ownerId if bot is ran as user
                _ownerId = _client.CurrentUser.Id;
                _client.MessageReceived += HandleSelfBotCommand;
            }
        }

        public async Task HandleBotCommand(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null) return;
            if (message.Author.IsBot) return;
            if (message.Author is SocketUnknownUser)
                return;

            // Mark where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message has a valid prefix, adjust argPos 
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix(_prefix, ref argPos))
            {
                // Create a Command Context
                try
                {
                    var context = new CommandContext(_client, message);

                    // Execute the Command, store the result
                    var result = await Commands.ExecuteAsync(context, argPos, _map).ConfigureAwait(false);

                    // If the command failed, notify the user unless no command was found
                    if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    {
                        await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}")
                            .ConfigureAwait(false);
                        Log.Debug($"**Error:** {result.ErrorReason}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + ": " + ex.Message);
                }
            }
            else
            {
                await _kiteChat.ParseChatAsync(parameterMessage, _client).ConfigureAwait(false);
            }
        }

        public async Task HandleSelfBotCommand(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null || message.Author.Id != _ownerId || message.Author is SocketUnknownUser)
                return;

            // Mark where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message has a valid prefix, adjust argPos 
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix(_prefix, ref argPos))
            {
                // Create a Command Context
                try
                {
                    var context = new CommandContext(_client, message);

                    // Execute the Command, store the result
                    var result = await Commands.ExecuteAsync(context, argPos, _map).ConfigureAwait(false);

                    // If the command failed, notify the user unless no command was found
                    if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    {
                        await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}")
                            .ConfigureAwait(false);
                        Log.Debug($"**Error:** {result.ErrorReason}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + ": " + ex.Message);
                }
            }
            else
            {
                await _kiteChat.ParseChatAsync(parameterMessage, _client).ConfigureAwait(false);
            }
        }
    }
}