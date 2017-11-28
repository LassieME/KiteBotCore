using System;
using System.Threading.Tasks;

namespace Discord.Addons.InteractiveCommands
{
    public class ReactionCallback
    {
        public bool ResumeAfterExecution { get; set; }

        public Func<IUser, Task> Function { get; set; }
    }
}
