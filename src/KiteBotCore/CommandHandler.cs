using Discord.Commands;
using Discord.WebSocket;
using KiteBotCore.Json;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace KiteBotCore
{
    public class CommandHandler
    {
        public CommandService Commands;
        private DiscordSocketClient _client;
        private KiteChat _kiteChat;
        private IServiceProvider _services;
        private char _prefix;
        private ulong _ownerId;

        public async Task InstallAsync(CommandService commandService, IServiceProvider map)
        {
            _services = map;
            Commands = commandService;
            _kiteChat = map.GetService<KiteChat>();

            _client = map.GetService<DiscordSocketClient>();

            var botSettings = map.GetService<BotSettings>();
            _ownerId = botSettings.OwnerId;
            _prefix = botSettings.CommandPrefix;

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
            if (!(parameterMessage is SocketUserMessage message)) return;
            if (message.Author is SocketUnknownUser) return;
            if (message.Author.IsBot)
            {
                if(message.Author.Id == _client.CurrentUser.Id)
                    KiteChat.BotMessages.Add(parameterMessage);
                return;
            }

            // Mark where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message has a valid prefix, adjust argPos 
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix(_prefix, ref argPos) || message.HasCharPrefix('-', ref argPos))
            {
                // Create a Command Context
                try
                {
                    var context = new CommandContext(_client, message);

                    // Execute the Command, store the result
                    var result = await Commands.ExecuteAsync(context, argPos, _services).ConfigureAwait(false);

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
            if (!(parameterMessage is SocketUserMessage message) || message.Author.Id != _ownerId || message.Author is SocketUnknownUser)
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
                    var result = await Commands.ExecuteAsync(context, argPos, _services).ConfigureAwait(false);

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