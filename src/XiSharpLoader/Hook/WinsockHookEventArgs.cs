using System;
using System.Diagnostics;

namespace XiSharpLoader.Hook
{
    internal class WinsockHookEventArgs : EventArgs
    {
        public WinsockHookEventArgs(Process process, IntPtr apiAdd, IntPtr hookAdd, IntPtr continueAdd)
        {
            Process = process;
            ApiAdd = apiAdd;
            HookAdd = hookAdd;
            ContinueAdd = continueAdd;
        }

        public Process Process { get; private set; }
        public IntPtr ApiAdd { get; private set; }
        public IntPtr HookAdd { get; private set; }
        public IntPtr ContinueAdd { get; private set; }

    }
}
