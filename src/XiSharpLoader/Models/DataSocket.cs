using System.Net.Sockets;

namespace XiSharpLoader.Models
{
    internal class DataSocket
    {
        public Socket Socket;
        public uint AccountId;
        public ulong LocalAddress;
        public ulong ServerAddress;
    }

}
