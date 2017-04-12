using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

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
            "Discord.WebSocket",
            "KiteBotCore"
        };

        private readonly DiscordSocketClient _client;
        private readonly CommandHandler _handler;

        private readonly ScriptOptions _options;
        private readonly CancellationTokenSource _token;

        public EvalService(IDependencyMap map)
        {
            _options = ScriptOptions.Default
                .AddReferences(GetAssemblies().ToArray())
                .AddImports(Imports);
            _token = new CancellationTokenSource();
            _client = map.Get<DiscordSocketClient>();
            _handler = map.Get<CommandHandler>();
        }

        public void PopToken()
        {
            _token.Cancel();
        }

        public async Task Evaluate(ICommandContext context, string script)
        {
            using (context.Channel.EnterTypingState())
            {
                IUserMessage working = await context.Channel.SendMessageAsync("**Evaluating**, just a sec...")
                    .ConfigureAwait(false);
                var globals = new ScriptGlobals
                {
                    handler = _handler,
                    client = _client,
                    context = context
                };
                script = script.Trim('`');
                try
                {
                    object eval = await CSharpScript
                        .EvaluateAsync(script, _options, globals, cancellationToken: _token.Token)
                        .ConfigureAwait(false);
                    await working.ModifyAsync(x => x.Content = eval.ToString()).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await working.ModifyAsync(x => x.Content = $"**Script Failed**\n{e.Message}").ConfigureAwait(false);
                }
            }
        }

        public IEnumerable<Assembly> GetAssemblies()
        {
            AssemblyName[] assemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies();
            foreach (AssemblyName a in assemblies)
            {
                Assembly asm = Assembly.Load(a);
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
        public ICommandContext context { get; internal set; }
        public SocketMessage msg => context.Message as SocketMessage;
        public SocketGuild guild => context.Guild as SocketGuild;
        public SocketGuildChannel channel => context.Channel as SocketGuildChannel;
    }
}