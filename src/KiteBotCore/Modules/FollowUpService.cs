using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace KiteBotCore.Modules
{
    public class FollowUpService //TODO: Make this non-static
    {
        private readonly List<FollowUp> _followUps = new List<FollowUp>();

        public void AddNewFollowUp(FollowUp followUp)
        {
            _followUps.Add(followUp);
        }

        internal void RemoveFollowUp(FollowUp followUp)
        {
            _followUps.Remove(followUp);
            followUp.Dispose();
        }
    }

    public class FollowUp : IDisposable
    {
        bool _disposed;

        private readonly Dictionary<string, Tuple<string, Func<EmbedBuilder>>> _dictionary;
        private readonly DiscordSocketClient _client;
        private readonly FollowUpService _followUpService;
        private readonly DateTime _creationTime;
        public readonly ulong User;
        private readonly ulong _channel;
        private readonly IUserMessage _messageToEdit;

        public FollowUp(IServiceProvider map, Dictionary<string, Tuple<string, Func<EmbedBuilder>>> dictionary, ulong user, ulong channel, IUserMessage messageToEdit)
        {
            _client = map.GetService<DiscordSocketClient>();
            _followUpService = map.GetService<FollowUpService>();
            _dictionary = dictionary;
            _creationTime = DateTime.Now;
            User = user;
            _channel = channel;
            _messageToEdit = messageToEdit;

            _client.MessageReceived += messageEventHandler;
        }

        private Task messageEventHandler(SocketMessage parameterMessage)
        {
            if (parameterMessage.Author.Id != User || parameterMessage.Channel.Id != _channel)
                return Task.CompletedTask;

            _ = Task.Run(async () =>
            {
                try
                {
                    var any = parameterMessage.Content.Split().Intersect(_dictionary.Keys);
                    var enumerable = any as string[] ?? any.ToArray();
                    Task post;

                    if (enumerable.Any())
                    {
                        var outputString = _dictionary[enumerable.FirstOrDefault()].Item1;
                        var outputEmbed = _dictionary[enumerable.FirstOrDefault()].Item2;

                        
                        if (outputEmbed != null)
                        {
                            post = _messageToEdit.ModifyAsync(x =>
                            {
                                x.Content = outputString;
                                x.Embed = outputEmbed().Build();
                            });

                        }
                        else
                        {
                            post = _messageToEdit.ModifyAsync(x => x.Content = outputString);
                        }
                        _followUpService.RemoveFollowUp(this);
                        await post.ConfigureAwait(false);
                    }
                    else if (DateTime.Now.Subtract(_creationTime) > TimeSpan.FromMinutes(2))
                    {
                        post = _messageToEdit.ModifyAsync(x => x.Content = "Command timed out.");
                        _followUpService.RemoveFollowUp(this);
                        await post.ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, ex.Message);
                }
            });
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _client.MessageReceived -= messageEventHandler;
            }
            _disposed = true;
        }
    }
}
