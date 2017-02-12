using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Modules;
using KiteBotCore.Modules.Giantbomb;
using KiteBotCore.Modules.Youtube;

namespace KiteBotCore
{
    public class KiteChat
    {
        public static Random RandomSeed;

		public static bool StartMarkovChain;

        private static string[] _greetings;

        public static LivestreamChecker StreamChecker;
        public static GiantBombVideoChecker GbVideoChecker;
        public static MultiTextMarkovChainHelper MultiDeepMarkovChains;
        public static List<SocketMessage> BotMessages = new List<SocketMessage>();

        public static string ChatDirectory = Directory.GetCurrentDirectory();
        public static string GreetingFileLocation = ChatDirectory + "/Content/Greetings.txt";

        private readonly string _gbapi;

        public KiteChat(DiscordSocketClient client, DiscordContextFactory db, bool markovbool, string gBapi, string ytApi, int streamRefresh, bool silentStartup, int videoRefresh, int depth)
        {
            _gbapi = gBapi;
            StartMarkovChain = markovbool;
            _greetings = File.ReadAllLines(GreetingFileLocation);
            RandomSeed = new Random();
            YoutubeModuleService.Init(ytApi, client);
            ReminderService.Init();

            if (streamRefresh > 3000) StreamChecker = new LivestreamChecker(gBapi, streamRefresh, silentStartup);
            if (videoRefresh > 3000) GbVideoChecker = new GiantBombVideoChecker(gBapi, videoRefresh);
            if (StartMarkovChain && depth > 0)MultiDeepMarkovChains = new MultiTextMarkovChainHelper(client, db, depth);
        }

        public async Task<bool> InitializeMarkovChainAsync()
        {
            var videoTask = Video.InitializeTask(_gbapi).ConfigureAwait(false); //Could be a longrunning Task,
            
            if (StartMarkovChain) await MultiDeepMarkovChains.InitializeAsync().ConfigureAwait(false); //This usually is a long running task, unless they bot has just recently been off

            await videoTask;
            
            return true;
        }

        public async Task ParseChatAsync(SocketMessage msg, IDiscordClient client)
        {
            if (msg.Author.Id == client.CurrentUser.Id)
            {
                BotMessages.Add(msg);
            }
            if (msg.Author.Id != client.CurrentUser.Id)
            {
                if(StartMarkovChain) await MultiDeepMarkovChains.Feed(msg);

                if (msg.Content.Contains("Mistake") && msg.Channel.Id == 96786127238725632)
                {
                    await msg.Channel.SendMessageAsync("Anime is a mistake " + msg.Author.Mention +".");
                }
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
                                    msg.Author.Username));
                    }
                    else if (msg.Content.ToLower().Contains("hi") ||
                             msg.Content.ToLower().Contains("hey") ||
                             msg.Content.ToLower().Contains("hello"))
                    {
                        await msg.Channel.SendMessageAsync(ParseGreeting(msg.Author.Username));
                    }
                }
            }
        }

        public static async Task<string> GetResponseUriFromRandomQlCrew()
		{
            string url = "http://qlcrew.com/main.php?anyone=anyone&inc%5B0%5D=&p=999&exc%5B0%5D=&per_page=15&random";

		    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            if (request != null)
            {
                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                return response?.ResponseUri.AbsoluteUri;
            }
            return "Couldn't load QLcrew's Random Link.";
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

        //grabs random greetings for user bekenel from a reddit profile
		//private async Task<bool> LoadBekGreetingsAsync()
		//{
		//	const string url = "https://www.reddit.com/user/UWotM8_SS";
		//	string htmlCode = null;
		//    try
		//    {
		//        using (HttpClient client = new HttpClient())
		//        {
		//            htmlCode = await client.GetStringAsync(url);
		//        }
		//    }
		//    catch (Exception e)
		//    {
		//        Console.WriteLine("Could not load Bek greetings, server not found: " + e.Message);
		//    }
		//    finally
		//    {
		//        var regex1 = new Regex(@"<div class=""md""><p>(?<quote>.+)</p>");
		//        if (htmlCode != null)
		//        {
		//            var matches = regex1.Matches(htmlCode);
		//            var stringArray = new string[matches.Count];
		//            var i = 0;
		//            foreach (Match match in matches)
		//            {
		//                var s = match.Groups["quote"].Value.Replace("&#39;", "'").Replace("&quot;", "\"");
		//                stringArray[i] = s;
		//                i++;
		//            }
		//            _bekGreetings = stringArray;
        //        }
		//    }
        //    return true;
        //}
    }
}
