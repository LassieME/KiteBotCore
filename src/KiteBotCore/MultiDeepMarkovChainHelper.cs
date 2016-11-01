using System;
using System.Collections.Generic;
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

namespace KiteBotCore
{
    public class MultiTextMarkovChainHelper
    {
        public int Depth;
        private Timer _timer;
        private readonly IMarkovChain _markovChain;
        private readonly IDiscordClient _client;
        private bool _isInitialized;
        private List<MarkovMessage> _jsonList = new List<MarkovMessage>();
        private JsonLastMessage _lastMessage;

        public static string RootDirectory = Directory.GetCurrentDirectory();
        public static string JsonLastMessageLocation => RootDirectory + "/Content/LastMessage.json";
        public static string JsonMessageFileLocation => RootDirectory + "/Content/messages.zip";

        public MultiTextMarkovChainHelper(int depth) : this(Program.Client, depth)
        {
        }

        public MultiTextMarkovChainHelper(IDiscordClient client, int depth)
        {
            Console.WriteLine("MultiTextMarkovChainHelper");
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
            _timer = new Timer(async e => await Save(), null, 3600000, 3600000);
        }

        public async Task<bool> Initialize()
        {
            Console.WriteLine("Initialize");
            if (!_isInitialized)
            {
                if (File.Exists(path: JsonMessageFileLocation))
                {
                    try
                    {
                        _jsonList = JsonConvert.DeserializeObject<List<MarkovMessage>>(Open());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex + ex.Message);
                    }

                    foreach (MarkovMessage message in _jsonList)
                    {
                        _markovChain.feed(message.M);//Any messages here have already been thru all the if checks, so we dont need to run through all of those again.
                    }
                    _isInitialized = true;
                    if (File.Exists(JsonLastMessageLocation))
                    {
                        try
                        {
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
                }
                else
                {
                    try
                    {
                        var guild = (await _client.GetGuildsAsync()).ToArray();
                        List<IMessage> list = new List<IMessage>(await GetMessagesFromChannel(guild.FirstOrDefault().Id, 20000));
                        //list.AddRange(await GetMessagesFromChannel(96786127238725632, 2500));
                        //list.AddRange(await GetMessagesFromChannel(94122326802571264, 2500));
                        foreach (IMessage message in list)
                        {
                            if (!string.IsNullOrWhiteSpace(message?.Content))
                            {
                                FeedMarkovChain(message);
                                var json = new MarkovMessage { M = message.Content };
                                _jsonList.Add(json);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex + ex.Message);
                    }
                }
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
                if (!string.IsNullOrWhiteSpace(message.Content) && !message.Content.Contains("http") && !message.Content.ToLower().Contains("testmarkov") && !message.Content.ToLower().Contains("getdunked") && message.MentionedUserIds.FirstOrDefault() != _client.CurrentUser.Id)//TODO: add back in is mentioning me check
                {
                    if (message.Content.Contains("."))
                    {
                        _markovChain.feed(message.Content); 
                    }
                    _markovChain.feed(message.Content + ".");
                    _jsonList.Add(new MarkovMessage() { M = message.Content});
                }
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
            SocketTextChannel channel = (SocketTextChannel) await _client.GetChannelAsync(channelId);
            var latestMessages = await channel.GetMessagesAsync(id, Direction.After, 10000).Flatten();
            var enumerable = latestMessages as IMessage[] ?? latestMessages.ToArray();
            return enumerable;
        }

        public async Task Save()
        {
            Console.WriteLine("Save");
            if (_isInitialized)
            {
                var text = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(_jsonList, Formatting.None));
                using (var fileStream = File.Open(JsonMessageFileLocation, FileMode.OpenOrCreate))
                {
                    using (var stream = new GZipStream(fileStream, CompressionLevel.Optimal))
                    {
                        stream.Write(text, 0, text.Length);     // Write to the `stream` here and the result will be compressed
                    }
                }
                var message = await ((SocketTextChannel)await _client.GetChannelAsync(85842104034541568)).GetMessagesAsync(1,RequestOptions.Default).Flatten();
                var x = new JsonLastMessage
                {
                    MessageId = message.First().Id,
                    ChannelId = message.First().Channel.Id
                };

                var lastmessageJson = JsonConvert.SerializeObject(x, Formatting.Indented);
                File.WriteAllText(JsonLastMessageLocation, lastmessageJson);
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
    }
}
