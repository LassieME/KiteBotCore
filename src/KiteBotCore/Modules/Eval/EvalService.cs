using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using System.Threading;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace KiteBotCore.Modules.Eval
{
    public class EvalService
    {
        public static readonly string[] Imports =
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Threading.Tasks",
            "System.Diagnostics",
            "System.IO",
            "Discord",
            "Discord.Commands",
            "Discord.WebSocket"            
        };

        private readonly ScriptOptions _options;
        private readonly CancellationTokenSource _token;
        private readonly DiscordSocketClient _client;
        private readonly CommandHandler _handler;
        public void PopToken() => _token.Cancel();

        public EvalService(IDependencyMap map)
        {
            _options = ScriptOptions.Default
                .AddReferences(GetAssemblies().ToArray())
                .AddImports(Imports);
            _token = new CancellationTokenSource();
            _client = map.Get<DiscordSocketClient>();
            _handler = map.Get<CommandHandler>();
        }

        public async Task Evaluate(CommandContext context, string script)
        {
            using (context.Channel.EnterTypingState())
            {
                var working = await context.Channel.SendMessageAsync("**Evaluating**, just a sec...");
                ScriptGlobals globals = new ScriptGlobals
                {
                    handler = _handler,
                    client = _client,
                    context = context,
                };
                script = script.Trim('`');
                try
                {
                    var eval =
                        await
                            CSharpScript.EvaluateAsync(script, _options, globals, cancellationToken: _token.Token);
                    await context.Channel.SendMessageAsync(eval.ToString());
                }
                catch (Exception e)
                {
                    await context.Channel.SendMessageAsync($"**Script Failed**\n{e.Message}");
                }
                finally
                {
                    await working.DeleteAsync();
                }
            }
        }

        public IEnumerable<Assembly> GetAssemblies()
        {
            var assemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies();
            foreach (var a in assemblies)
            {
                var asm = Assembly.Load(a);
                yield return asm;
            }
            yield return Assembly.GetEntryAssembly();
            yield return typeof(ILookup<string, string>).GetTypeInfo().Assembly;
        }
    }

    public class ScriptGlobals
    {
        public CommandHandler handler { get; internal set; }
        public DiscordSocketClient client { get; internal set; }
        public CommandContext context { get; internal set; }
        public SocketMessage msg => context.Message as SocketMessage;
        public SocketGuild guild => context.Guild as SocketGuild;
        public SocketGuildChannel channel => context.Channel as SocketGuildChannel;
    }
}