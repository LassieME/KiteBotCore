using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord.Commands;
using Serilog;

namespace KiteBotCore.Modules.Eval
{
    public class Eval : ModuleBase
    {
        public IServiceProvider Services { get; set; }

        private Stopwatch _stopwatch;
        protected override void BeforeExecute()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute()
        {
            _stopwatch.Stop();
            Log.Debug($"Eval Command: {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }

        [Command("eval", RunMode = RunMode.Sync)]
        [Summary("evaluates C# script")]
        [RequireOwner]
        public async Task EvalCommand([Remainder]string script)
        {
            var evalService = new EvalService(Services);
            var scriptTask = evalService.Evaluate(Context, script);
            await Task.Delay(10000).ConfigureAwait(false);
            if (!scriptTask.IsCompleted) evalService.PopToken();
        }

        //public static unsafe void EvalCommand()
        //{
        //    var assembly = typeof(Foo).GetTypeInfo().Assembly; //let's grab the current in-memory assembly
        //    if (assembly.TryGetRawMetadata(out byte* b, out int length))
        //    {
        //        var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)b, length);
        //        var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
        //        var reference = assemblyMetadata.GetReference();

        //        var opts = ScriptOptions.Default.AddImports("ConsoleApplication").AddReferences(reference);
        //        var script = CSharpScript.Create("var foo = new Foo();", opts);
        //        var result = script.RunAsync().Result; //runs fine, possible to use main application types in the script
        //    }
        //}

        //public class Foo
        //{
        //    public int Bar { get; set; }
        //}
    }
}

