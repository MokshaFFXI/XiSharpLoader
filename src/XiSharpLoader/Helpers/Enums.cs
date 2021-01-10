using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("XiSharpLoaderTests")]
namespace XiSharpLoader.Helpers
{
    internal class Enums
    {
        public enum PolLanguage
        {
            Japanese = 0,
            English = 1,
            European = 2
        };

        public enum AccountResult
        {
            Login_Success = 0x001,
            Login_Error = 0x002,
            Create_Success = 0x003,
            Create_Taken = 0x004,
            Create_Disabled = 0x008,
            Create_Error = 0x009,
            PassChange_Request = 0x005,
            PassChange_Success = 0x006,
            PassChange_Error = 0x007,
        };
    }
}
