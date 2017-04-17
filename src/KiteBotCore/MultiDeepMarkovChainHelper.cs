using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MarkovSharp.TokenisationStrategies;
using Newtonsoft.Json;
using Serilog;

namespace KiteBotCore
{
    public class MultiTextMarkovChainHelper
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordContextFactory _dbFactory;
        private readonly StringMarkov _markovChain;

        private readonly string _rootDirectory = Directory.GetCurrentDirectory();
        private readonly SemaphoreSlim _semaphore;
        private KiteBotDbContext _db;
        private bool _isInitialized;
        private JsonLastMessage _lastMessage;

        // ReSharper disable once NotAccessedField.Local
        private Timer _timer;

        public int Depth;

        public MultiTextMarkovChainHelper(DiscordSocketClient client, DiscordContextFactory dbFactory, int depth)
        {
            _semaphore = new SemaphoreSlim(0, 1);
            _dbFactory = dbFactory;
            _db = _dbFactory.Create(new DbContextFactoryOptions());
            _client = client;
            Depth = depth;

            if (depth > 0)
                _markovChain = new StringMarkov(depth);

            _timer = new Timer(async e => await SaveAsync(), null, 600000, 600000);
        }

        private string JsonLastMessageLocation => _rootDirectory + "/Content/LastMessage.json";

        public static int AmountOfFails { get => amountOfFails; set => amountOfFails = value; }

        internal async Task InitializeAsync()
        {
            Console.WriteLine("Initialize");
            if (!_isInitialized)
            {
                try
                {
                    if (File.Exists(JsonLastMessageLocation))
                        try
                        {
                            foreach (Message message in _db.Messages)
                                FeedMarkovChain(message);
                            _semaphore.Release();
                            string s = File.ReadAllText(JsonLastMessageLocation);
                            _lastMessage = JsonConvert.DeserializeObject<JsonLastMessage>(s);
                            var list =
                                new List<IMessage>(await DownloadMessagesAfterIdAsync(_lastMessage.MessageId,
                                    _lastMessage.ChannelId));
                            foreach (IMessage message in list)
                                await FeedMarkovChain(message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex + ex.Message);
                        }
                    else
                        try
                        {
                            var guilds = _client.Guilds;
                            var list =
                                new List<IMessage>(await GetMessagesFromChannelAsync(guilds.FirstOrDefault().Id, 1000));
                            _semaphore.Release();

                            var dbGuilds = await _db.Guilds
                                .Include(g => g.Channels)
                                .Include(g => g.Users)
                                .ToListAsync();

                            List<Channel> channels = new List<Channel>();
                            List<User> users = new List<User>();
                            foreach (var guild in dbGuilds)
                            {
                                channels.AddRange(guild.Channels);
                                users.AddRange(guild.Users);
                            }

                            foreach (IMessage message in list)
                                await FeedMarkovChain(message, channels, users);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex + ex.Message);
                        }
                    _isInitialized = true;
                    await SaveAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + ex.Message);
                }
            }
        }

        internal Task Feed(IMessage message)
        {
            return FeedMarkovChain(message);
        }

        public string GetSequence()
        {
            if (_isInitialized)
                try
                {
                    return _markovChain.Walk().First();
                }
                catch (NullReferenceException ex)
                {
                    Log.Warning("Nullref fun {0}", ex.Message);
                    return GetSequence();
                }
            return "I'm not ready yet Senpai!";
        }

        private async Task FeedMarkovChain(IMessage message, List<Channel> channels = null, List<User> users = null)
        {
            if (!message.Author.IsBot)
                if (!string.IsNullOrWhiteSpace(message.Content) && !message.Content.Contains("http") &&
                    !message.Content.ToLower().Contains("testmarkov") &&
                    !message.Content.ToLower().Contains("getdunked") &&
                    !message.Content.ToLower().StartsWith("!") &&
                    !message.Content.ToLower().StartsWith("~") &&
                    message.MentionedUserIds.FirstOrDefault() !=
                    _client.CurrentUser.Id)
                {
                    _markovChain.Learn(message.Content);

                    try
                    {
                        Channel channel = channels == null
                            ? await _db.Channels.FirstAsync(x => x.Id == message.Channel.Id)
                                .ConfigureAwait(false)
                            : channels.Find(x => x.Id == message.Channel.Id);

                        User user = users == null
                            ? await _db.Users.FirstAsync(x => x.Id == message.Author.Id).ConfigureAwait(false)
                            : users.Find(x => x.Id == message.Author.Id);

                        var entityMessage = new Message
                        {
                            Content = message.Content,
                            Id = message.Id,
                            Channel = channel,
                            User = user
                        };

                        Debug.Assert(entityMessage.Content != null && entityMessage.Channel != null &&
                                     entityMessage.User != null);
                        
                        _db.Messages.Add(entityMessage);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Log.Verbose("An Identical MessageID is already in the database :" + ex.Message);
                    }
                    catch (NullReferenceException ex)
                    {
                        Log.Debug(ex + ex.Message + "\n" + amountOfFails++);
                    }
                }
        }

        private static int amountOfFails = 0;

        private void FeedMarkovChain(Message message)
        {
            if (!string.IsNullOrWhiteSpace(message.Content) && !message.Content.Contains("http")
                && !message.Content.ToLower().Contains("testmarkov") && !message.Content.ToLower().Contains("tm")
                && !message.Content.ToLower().Contains("getdunked") &&
                !message.Content.Contains(_client.CurrentUser.Id.ToString()))
            {
                if (message.Content.Contains("."))
                    _markovChain.Learn(message.Content);
                _markovChain.Learn(message.Content + ".");
            }
        }

        private async Task<IEnumerable<IMessage>> GetMessagesFromChannelAsync(ulong channelId, int i)
        {
            Console.WriteLine("GetMessagesFromChannel");
            var channel = (SocketTextChannel) _client.GetChannel(channelId);
            IEnumerable<IMessage> latestMessages = await channel.GetMessagesAsync(i).Flatten();
            IMessage[] enumerable = latestMessages as IMessage[] ?? latestMessages.ToArray();
            return enumerable;
        }

        private async Task<IEnumerable<IMessage>> DownloadMessagesAfterIdAsync(ulong id, ulong channelId)
        {
            Console.WriteLine("DownloadMessagesAfterId");
            var channel = (SocketTextChannel)_client.GetChannel(channelId);
            IEnumerable<IMessage> latestMessages = await channel.GetMessagesAsync(id, Direction.After, 10000).Flatten();
            IMessage[] enumerable = latestMessages as IMessage[] ?? latestMessages.ToArray();
            return enumerable;
        }

        internal async Task SaveAsync()
        {
            Console.WriteLine("SaveAsync");
            if (_isInitialized)
            {
                try
                {
                    Log.Debug("_semaphore.WaitAsync() in SaveAsync");
                    await _semaphore.WaitAsync();
                    await _db.SaveChangesAsync();

                }
                catch (Exception ex)
                {
                    Log.Error(ex + ex.Message);
                }
                finally
                {
                    Log.Debug("_semaphore.Release() in SaveAsync()");
                    _semaphore.Release();
                }
                try
                {
                    Message latestMessage = _db.Messages.Local.OrderByDescending(y => y.Id)
                            .FirstOrDefault(z => z?.Channel?.Id == 85842104034541568);

                    if (latestMessage != null)
                    {
                        var json = new JsonLastMessage
                        {
                            MessageId = latestMessage.Id,
                            ChannelId = latestMessage.Channel.Id
                        };
                        string lastmessageJson = JsonConvert.SerializeObject(json, Formatting.Indented);
                        File.WriteAllText(JsonLastMessageLocation, lastmessageJson);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex + ex.Message);
                }
                finally
                {
                    _db.Dispose();
                    _db = _dbFactory.Create(new DbContextFactoryOptions());
                }
            }
        }

        internal async Task<List<Message>> GetFullDatabase(ulong channelId)
        {
            return await _db.Messages.Where(x => x.Channel.Id == channelId).ToListAsync().ConfigureAwait(false);
        }

        internal async Task<List<Message>> GetFullDatabase()
        {
            return await _db.Messages.ToListAsync().ConfigureAwait(false);
        }

        internal async Task RemoveItemAsync(Message message)
        {
            _db.Remove(message);
            await _db.SaveChangesAsync().ConfigureAwait(false);
        }

        public void SetDepth(int depth)
        {
            _markovChain.Retrain(depth);
            Depth = depth;
        }
    }
}