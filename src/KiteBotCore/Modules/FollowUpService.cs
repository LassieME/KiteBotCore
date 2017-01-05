using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace KiteBotCore.Modules
{
    public static class FollowUpService
    {
        private static readonly List<FollowUp> FollowUps = new List<FollowUp>();

        public static void AddNewFollowUp(FollowUp followUp)
        {
            FollowUps.Add(followUp);
        }

        internal static void RemoveFollowUp(FollowUp followUp)
        {
            FollowUps.Remove(followUp);
            followUp.Dispose();
        }
    }

    public class FollowUp : IDisposable
    {
        bool _disposed;

        private readonly Dictionary<string, Tuple<string, EmbedBuilder>> _dictionary;
        private readonly DiscordSocketClient _client;
        private readonly DateTime _creationTime;
        public readonly ulong User;
        private readonly ulong _channel;
        private readonly IUserMessage _messageToEdit;

        public FollowUp(IDependencyMap map, Dictionary<string, Tuple<string, EmbedBuilder>> dictionary, ulong user, ulong channel, IUserMessage messageToEdit)
        {
            _client = map.Get<DiscordSocketClient>();
            _dictionary = dictionary;
            _creationTime = DateTime.Now;
            User = user;
            _channel = channel;
            _messageToEdit = messageToEdit;

            _client.MessageReceived += messageEventHandler;
        }

        private async Task messageEventHandler(SocketMessage parameterMessage)
        {
            var any = parameterMessage.Content.Split().Intersect(_dictionary.Keys);
            var enumerable = any as string[] ?? any.ToArray();
            if (parameterMessage.Author.Id == User && parameterMessage.Channel.Id == _channel && enumerable.Any())
            {
                var outputString = _dictionary[enumerable.FirstOrDefault()].Item1;
                var outputEmbed = _dictionary[enumerable.FirstOrDefault()].Item2;
                if (outputEmbed != null)
                {
                    await _messageToEdit.ModifyAsync(x => { x.Content = outputString; x.Embed = outputEmbed.Build(); });
                }
                else
                {
                    await _messageToEdit.ModifyAsync(x => x.Content = outputString);
                }

                FollowUpService.RemoveFollowUp(this);
            }
            else if(DateTime.Now.Subtract(_creationTime) > TimeSpan.FromMinutes(2))
            {
                await _messageToEdit.ModifyAsync(x => x.Content = "Command timed out.");
                FollowUpService.RemoveFollowUp(this);
            }
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
