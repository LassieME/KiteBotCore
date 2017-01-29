﻿using System;
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
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IDependencyMap _map;
        private char _prefix;
        private ulong _ownerId;

        public async Task InstallAsync(IDependencyMap map, char prefix)
        {
            _prefix = prefix;
            // Create Command Service, inject it into Dependency Map
            _client = map.Get<DiscordSocketClient>();
            _commands = new CommandService();
            map.Add(_commands);
            _map = map;
            if (map.TryGet(out BotSettings botSettings))
            {
                _ownerId = botSettings.OwnerId;
            }

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            
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
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix(_prefix, ref argPos))
            {
                if (System.Diagnostics.Debugger.IsAttached && parameterMessage.Author.Id != _ownerId)
                    return;
                // Create a Command Context
                try
                {
                    var context = new CommandContext(_client, message);

                    // Execute the Command, store the result
                    var result = await _commands.ExecuteAsync(context, argPos, _map);

                    // If the command failed, notify the user unless no command was found
                    if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    {
                        await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
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
                await _map.Get<KiteChat>().ParseChatAsync(parameterMessage, _map.Get<DiscordSocketClient>());
            }
        }
    }
}