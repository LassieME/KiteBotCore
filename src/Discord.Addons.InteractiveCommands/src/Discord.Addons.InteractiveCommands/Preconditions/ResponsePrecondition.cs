using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Addons.InteractiveCommands
{
    public abstract class ResponsePrecondition
    {
        public abstract Task<ResponsePreconditionResult> CheckPermissions(ResponseContext context);
    }
}
