using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json;

namespace KiteBotCore.Modules
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    // Inherit from PreconditionAttribute
    public class RequireOrAttribute : PreconditionAttribute
    {
        private readonly object[] _preconditions;

        public RequireOrAttribute(Server enumer, params Type[] preconditions)
        {
            _preconditions = new object[preconditions.Length];
            for (int i = 0; i < preconditions.Length; i++)
            {
                _preconditions[i] = preconditions[i].GetConstructor(Type.EmptyTypes).Invoke(new object[]{enumer});
            }
        }

        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo executingCommand, IDependencyMap map)
        {
            if (_preconditions.Length == 0)
                return PreconditionResult.FromError("No one can use this command, plz fix.");
            
            foreach (var pre in _preconditions)
            {
                var preResult = await ((PreconditionAttribute)pre).CheckPermissions(context, executingCommand, map);
                if (!preResult.IsSuccess)
                {
                    return preResult;
                }
            }
            return  PreconditionResult.FromSuccess();
        }
    }
}
