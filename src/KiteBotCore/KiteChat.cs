using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Modules.Youtube;

namespace KiteBotCore
{
    public class KiteChat
    {
        public static Random RandomSeed;

		public static bool StartMarkovChain;

        private static string[] _greetings;

        //public static LivestreamChecker StreamChecker;
        public static GiantBombVideoChecker GbVideoChecker;
        public static MultiTextMarkovChainHelper MultiDeepMarkovChains;
        public static List<SocketMessage> BotMessages = new List<SocketMessage>();

        public static string ChatDirectory = Directory.GetCurrentDirectory();
        public static string GreetingFileLocation = ChatDirectory + "/Content/Greetings.txt";

        public KiteChat(DiscordSocketClient client, DiscordContextFactory db, bool markovbool, string gBapi, string ytApi, int streamRefresh, bool silentStartup, int videoRefresh, int depth, bool mChainShouldDownload)
        {
            StartMarkovChain = markovbool;
            _greetings = File.ReadAllLines(GreetingFileLocation);
            RandomSeed = new Random();
            YoutubeModuleService.Init(ytApi, client);

            //if (streamRefresh > 30000) StreamChecker = new LivestreamChecker(client, gBapi, streamRefresh, silentStartup);
            if (videoRefresh > 30000) GbVideoChecker = new GiantBombVideoChecker(client, gBapi, videoRefresh);
            if (StartMarkovChain && depth > 0)MultiDeepMarkovChains = new MultiTextMarkovChainHelper(client, db, depth, mChainShouldDownload);
        }

        public async Task InitializeMarkovChainAsync()
        {
            if (StartMarkovChain) await MultiDeepMarkovChains.InitializeAsync().ConfigureAwait(false); //This usually is a long running task, unless they bot has just recently been off
        }

        public async Task ParseChatAsync(SocketMessage msg, IDiscordClient client)
        {
            if (msg.Author.Id == client.CurrentUser.Id)
            {
                BotMessages.Add(msg);
            }
            else if (msg.Author.Id != client.CurrentUser.Id)
            {
                if(StartMarkovChain && msg.Channel is IGuildChannel) await MultiDeepMarkovChains.Feed(msg).ConfigureAwait(false);

                else if (msg.MentionedUsers.Any(x => x.Id == client.CurrentUser.Id))
                {
                    if (msg.Content.ToLower().Contains("fuck you") ||
                             msg.Content.ToLower().Contains("fuckyou"))
                    {
                        List<string> possibleResponses = new List<string>
                        {
                            "Hey fuck you too USER!",
                            "I bet you'd like that wouldn't you USER?",
                            "No, fuck you USER!",
                            "Fuck you too USER!"
                        };

                        await
                            msg.Channel.SendMessageAsync(
                                possibleResponses[RandomSeed.Next(0, possibleResponses.Count)].Replace("USER",
                                    msg.Author.Username)).ConfigureAwait(false);
                    }
                    else if (msg.Content.ToLower().Contains("hi") ||
                             msg.Content.ToLower().Contains("hey") ||
                             msg.Content.ToLower().Contains("hello"))
                    {
                        await msg.Channel.SendMessageAsync(ParseGreeting(msg.Author.Username)).ConfigureAwait(false);
                    }
                }
            }
        }
        
        //returns a greeting from the greetings.txt list on a per user or generic basis
	    private string ParseGreeting(string userName)
        {
		    List<string> possibleResponses = new List<string>();

	        for (int i = 0; i < _greetings.Length - 2; i += 2)
	        {
	            if (userName.ToLower().Contains(_greetings[i]))
	            {
	                possibleResponses.Add(_greetings[i + 1]);
	            }
	        }

	        if (possibleResponses.Count == 0)
	        {
	            for (int i = 0; i < _greetings.Length - 2; i += 2)
	            {
	                if (_greetings[i] == "generic")
	                {
	                    possibleResponses.Add(_greetings[i + 1]);
	                }
	            }
	        }

	        //return a random response from the context provided, replacing the string "USER" with the appropriate username
	        return possibleResponses[RandomSeed.Next(0, possibleResponses.Count)].Replace("USER", userName);
        }
    }
}
