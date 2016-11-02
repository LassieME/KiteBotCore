using System.Threading.Tasks;
using Discord.Commands;

namespace KiteBotCore.Modules.Eval
{
    public class Eval : ModuleBase 
    {
        private readonly IDependencyMap _map;

        public Eval(IDependencyMap map)
        {
            _map = map;
        }

        [Command("eval",RunMode = RunMode.Sync)]
        [Summary("evaluates C# script")]
        [RequireOwner]
        public async Task EvalCommand([Remainder]string script)
        {
            var evalService = new EvalService(_map);
            var scriptTask = evalService.Evaluate(Context, script);
            await Task.Delay(10000);
            if(!scriptTask.IsCompleted) evalService.PopToken();
        }
        
        /*public static unsafe void EvalCommand()
                {
                    byte* b;
                    int length;
                    var assembly = typeof(Foo).GetTypeInfo().Assembly; //let's grab the current in-memory assembly
                    if (assembly.TryGetRawMetadata(out b, out length))
                    {
                        var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)b, length);
                        var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                        var reference = assemblyMetadata.GetReference();
        
                        var opts = ScriptOptions.Default.AddImports("ConsoleApplication").AddReferences(reference);
                        var script = CSharpScript.Create("var foo = new Foo();", opts);
                        var result = script.RunAsync().Result; //runs fine, possible to use main application types in the script
                    }
        
                    Console.WriteLine("Hello World!");
                }
                
                public class Foo
                {
                    public int Bar { get; set; }
                }*/
    }
}

