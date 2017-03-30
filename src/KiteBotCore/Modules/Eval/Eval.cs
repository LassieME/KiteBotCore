using System.Threading.Tasks;
using Discord.Commands;

namespace KiteBotCore.Modules.Eval
{
    public class Eval : ModuleBase
    {
        public IDependencyMap Map { get; set; }

        [Command("eval", RunMode = RunMode.Sync)]
        [Summary("evaluates C# script")]
        [RequireOwner]
        public async Task EvalCommand([Remainder]string script)
        {
            var evalService = new EvalService(Map);
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

