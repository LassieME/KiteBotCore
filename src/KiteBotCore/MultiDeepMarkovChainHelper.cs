using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Json;
using MarkovChain;
using Newtonsoft.Json;
using Serilog;

namespace KiteBotCore
{
    public class MultiTextMarkovChainHelper
    {
        public int Depth;
        // ReSharper disable once NotAccessedField.Local
        private Timer _timer;
        private readonly IMarkovChain _markovChain;
        private readonly IDiscordClient _client;
        private bool _isInitialized;
        private JsonLastMessage _lastMessage;
        private readonly MarkovChainMContext _db;
        private static SemaphoreSlim _semaphore;

        public static string RootDirectory = Directory.GetCurrentDirectory();
        public static string JsonLastMessageLocation => RootDirectory + "/Content/LastMessage.json";
        public static string JsonMessageFileLocation => RootDirectory + "/Content/messages.zip";

        public MultiTextMarkovChainHelper(int depth) : this(Program.Client, depth)
        {
        }

        public MultiTextMarkovChainHelper(IDiscordClient client, int depth)
        {
            _semaphore = new SemaphoreSlim(0, 1);
            _db = new MarkovChainMContext();
            _client = client;
            Depth = depth;
            switch (depth)
            {
                case 1:
                    _markovChain = new TextMarkovChain();
                    break;
                case 2:
                    _markovChain = new DeepMarkovChain();
                    break;
                default:
                    if (depth < 2)
                    {
                        _markovChain = new MultiDeepMarkovChain(Depth);
                    }
                    else
                    {
                        _markovChain = new TextMarkovChain();
                    }
                    break;
            }
            _timer = new Timer(async e => await SaveAsync(), null, 600000, 600000);
        }

        public async Task<bool> Initialize()
        {
            Console.WriteLine("Initialize");
            if (!_isInitialized)
            {
                if (File.Exists(JsonLastMessageLocation))
                {
                    try
                    {
                        foreach (MarkovMessage message in _db.Messages)
                        {
                            FeedMarkovChain(message);
                        }
                        _semaphore.Release();
                        string s = File.ReadAllText(JsonLastMessageLocation);
                        _lastMessage = JsonConvert.DeserializeObject<JsonLastMessage>(s);
                        List<IMessage> list = new List<IMessage>(await DownloadMessagesAfterId(_lastMessage.MessageId, _lastMessage.ChannelId));
                        foreach (IMessage message in list)
                        {
                            FeedMarkovChain(message);
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("fucking Last Message JSON is killing me");
                    }
                }
                else
                {
                    try
                    {
                        var guild = (await _client.GetGuildsAsync()).ToArray();
                        List<IMessage> list = new List<IMessage>(await GetMessagesFromChannel(guild.FirstOrDefault().Id, 1000));
                        foreach (IMessage message in list)
                        {
                            if (!string.IsNullOrWhiteSpace(message?.Content))
                            {
                                FeedMarkovChain(message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex + ex.Message);
                    }
                }
                await SaveAsync();
                return _isInitialized = true;
            }
            return _isInitialized;
        }

        public void Feed(IMessage message)
        {
            FeedMarkovChain(message);
        }

        public string GetSequence()
        {
            if (_isInitialized)
            {
                try
                {
                    return _markovChain.generateSentence();
                }
                catch (NullReferenceException ex)
                {
                    Console.WriteLine("Nullref fun " + ex.Message);
                    return GetSequence();
                }
            }
            return "I'm not ready yet Senpai!";
        }

        private void FeedMarkovChain(IMessage message)
        {
            if (!message.Author.IsBot)
            {
                if (!string.IsNullOrWhiteSpace(message.Content) && !message.Content.Contains("http") && !message.Content.ToLower().Contains("testmarkov") && !message.Content.ToLower().Contains("getdunked") && message.MentionedUserIds.FirstOrDefault() != _client.CurrentUser.Id)
                {
                    if (message.Content.Contains("."))
                    {
                        _markovChain.feed(message.Content);
                    }
                    _markovChain.feed(message.Content + ".");
                    var json = new MarkovMessage { M = message.Content, Id = message.Id };
                    //_jsonList.Add(json);
                    try
                    {
                        //if(!_db.Messages.Any(x => x.Id == json.Id))
                        Log.Verbose("_semaphore.Wait in add()");
                        _semaphore.Wait();
                        _db.Messages.Add(json);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Log.Verbose("An Identical MessageID is already in the database :" + ex.Message);
                    }
                    finally
                    {
                        Log.Verbose("_semaphore.Release() in add()");
                        _semaphore.Release();
                    }
                }
            }
        }

        private void FeedMarkovChain(MarkovMessage message)
        {
            if (!string.IsNullOrWhiteSpace(message.M) && !message.M.Contains("http") && !message.M.ToLower().Contains("testmarkov") && !message.M.ToLower().Contains("tm") && !message.M.ToLower().Contains("getdunked") && !message.M.Contains(Program.Client.CurrentUser.Id.ToString()))
            {
                if (message.M.Contains("."))
                {
                    _markovChain.feed(message.M);
                }
                _markovChain.feed(message.M + ".");
                var json = new MarkovMessage { M = message.M, Id = message.Id };
            }
        }

        private async Task<IEnumerable<IMessage>> GetMessagesFromChannel(ulong channelId, int i)
        {
            Console.WriteLine("GetMessagesFromChannel");
            SocketTextChannel channel = (SocketTextChannel)await _client.GetChannelAsync(channelId);
            var latestMessages = await channel.GetMessagesAsync(i).Flatten();
            var enumerable = latestMessages as IMessage[] ?? latestMessages.ToArray();
            return enumerable;
        }

        private async Task<IEnumerable<IMessage>> DownloadMessagesAfterId(ulong id, ulong channelId)
        {
            Console.WriteLine("DownloadMessagesAfterId");
            SocketTextChannel channel = (SocketTextChannel)await _client.GetChannelAsync(channelId);
            var latestMessages = await channel.GetMessagesAsync(id, Direction.After, 10000).Flatten();
            var enumerable = latestMessages as IMessage[] ?? latestMessages.ToArray();
            return enumerable;
        }

        public async Task SaveAsync()
        {
            Console.WriteLine("SaveAsync");
            if (_isInitialized)
            {
                try
                {
                    Log.Debug("_semaphore.WaitAsync() in SaveAsync");
                    await _semaphore.WaitAsync();
                    await _db.SaveChangesAsync();

                    var message = await ((SocketTextChannel)await _client.GetChannelAsync(85842104034541568)).GetMessagesAsync(1, RequestOptions.Default).Flatten();
                    var x = new JsonLastMessage
                    {
                        MessageId = message.First().Id,
                        ChannelId = message.First().Channel.Id
                    };

                    var lastmessageJson = JsonConvert.SerializeObject(x, Formatting.Indented);
                    File.WriteAllText(JsonLastMessageLocation, lastmessageJson);

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
            }
        }

        private static string Open()
        {
            Console.WriteLine("Open");
            byte[] file = File.ReadAllBytes(JsonMessageFileLocation);
            Console.WriteLine("file");
            using (var stream = new GZipStream(new MemoryStream(file), CompressionMode.Decompress))
            {
                Console.WriteLine("stream");
                const int size = 4096;
                byte[] buffer = new byte[size];

                using (MemoryStream memory = new MemoryStream())
                {
                    int count;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    } while (count > 0);
                    Console.WriteLine("return");
                    return Encoding.Unicode.GetString(memory.ToArray());
                }
            }
        }

        public ImmutableList<MarkovMessage> GetFullDatabase()
        {
            return _db.Messages.ToImmutableList();
        }
    }
}
