using System.Threading;

namespace XiSharpLoader.Models
{
    internal class SharedState
    {
        public bool IsRunning = false;
        public Mutex Mutex;
        // TODO: Implement ConditionalVaraiable type
        // public ConditionVariable conditionVariable;
    }
}
