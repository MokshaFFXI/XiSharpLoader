using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using XiSharpLoader.Helpers;
using XiSharpLoader.Models;

namespace XiSharpLoader.Network
{
    internal class Network
    {
        /// <summary>
        /// Data communication between the local client and the game server.
        /// </summary>
        /// <param name="socket">Communications socket.</param>
        /// <param name="server">Server address to connect.</param>
        /// <param name="characterList">Character list.</param>
        /// <param name="sharedState">Shared thread state (bool, mutex, condition_variable).</param>
        public static void FFXiDataComm(DataSocket socket, string server, char[] characterList, SharedState sharedState)
        {
            /* Attempt to create connection to the server.. */
            if (!CreateConnection(socket, server, "54230"))
            {
                IoHelper.WriteLineWithColor(IoHelper.ERROR, "Failed connection to Server");
                NotifyShutdown(sharedState);
                return;
            }

            int sendSize = 0;
            byte[] byteReceiveBuffer = new byte[4096];
            byte[] byteSendBuffer = new byte[4096];

            while (sharedState.IsRunning)
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint senderRemote = sender;
                                
                try
                {
                    socket.Socket.ReceiveFrom(byteReceiveBuffer, ref senderRemote);
                }
                catch (Exception ex)
                {
                    IoHelper.WriteLineWithColor(IoHelper.ERROR, $"Failed recvfrom: {ex.Message}");
                    NotifyShutdown(sharedState);
                    return;
                }

                switch (Encoding.ASCII.GetString(byteReceiveBuffer).ToCharArray()[0])
                {
                    case '\x0001':
                        // TODO: This doesnt seem to be working correctly                     
                        byteSendBuffer[0] = 0xA1;
                        Buffer.BlockCopy(BitConverter.GetBytes(socket.AccountId), 0, byteSendBuffer, 0x01, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(socket.ServerAddress), 0, byteSendBuffer, 0x05, 4);
                        IoHelper.WriteLineWithColor(IoHelper.WARNING, "Sending account id..");
                        sendSize = 9;
                        break;

                    case '\x0002':
                    case '\x0015':
                        // TODO: Verify the this actually works
                        //memcpy(sendBuffer, (char*)"\xA2\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x58\xE0\x5D\xAD\x00\x00\x00\x00", 25);
                        byteSendBuffer = Encoding.ASCII.GetBytes("\xA2\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x58\xE0\x5D\xAD\x00\x00\x00\x00");
                        IoHelper.WriteLineWithColor(IoHelper.WARNING, "Sending key..");
                        sendSize = 25;
                        break;

                    case '\x0003':
                        IoHelper.WriteLineWithColor(IoHelper.WARNING, "Receiving character list..");
                        for (int x = 0; x <= byteReceiveBuffer[1]; x++)
                        {
                            characterList[0x00 + (x * 0x68)] = (char)1;
                            characterList[0x02 + (x * 0x68)] = (char)1;
                            characterList[0x10 + (x * 0x68)] = (char)x;
                            characterList[0x11 + (x * 0x68)] = '\x80';
                            characterList[0x18 + (x * 0x68)] = '\x20';
                            characterList[0x28 + (x * 0x68)] = '\x20';

                            // TODO: Port this
                            //memcpy(characterList + 0x04 + (x * 0x68), byteReceiveBuffer + (0x10 * (x + 1)) + 0x04, 4); // Character Id
                            //memcpy(characterList + 0x08 + (x * 0x68), byteReceiveBuffer + 0x10 * (x + 1), 4); // Content Id
                        }
                        sendSize = 0;
                        break;
                }

                if (sendSize == 0)
                {
                    continue;
                }

                /* Send the response buffer to the server.. */
                int result = socket.Socket.SendTo(byteSendBuffer, sendSize, SocketFlags.None, senderRemote);
                if (sendSize == 72 || result == (int)SocketError.SocketError || sendSize == -1)
                {
                    // TODO: GetLastError and put in message
                    IoHelper.WriteLineWithColor(IoHelper.ERROR, $"Failed sendto: ");
                    Console.WriteLine("Server connection done; disconnecting!");
                    NotifyShutdown(sharedState);
                    return;
                }

                sendSize = 0;
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Starts the local listen server to lobby server communications.
        /// </summary>
        /// <param name="socket">Socket reference to accept communications.</param>
        /// <param name="client">Client Socket reference to listen on.</param>
        /// <param name="lobbyServerPort">Lobby server port.</param>
        /// <param name="sharedState">Shared thread state (bool, mutex, condition_variable).</param>
        public static void PolServer(Socket socket, Socket client, string lobbyServerPort, SharedState sharedState)
        {
            //TODO: Fill in
            /* Attempt to create listening server.. */
            if (!CreateListenServer(ref socket, ProtocolType.Tcp, lobbyServerPort))
            {
                // TODO: Log actual error
                IoHelper.WriteLineWithColor(IoHelper.ERROR, $"Listen failed: ");
                NotifyShutdown(sharedState);
                return;
            }

            Thread polDataComm;

            while (sharedState.IsRunning)
            {
                /* Attempt to accept incoming connections.. */
                try
                {
                    client = socket.Accept();

                    /* Start data communication for this client.. */
                    PolDataComm(ref client, sharedState);
                    /* Shutdown the client socket.. */
                    CleanupSocket(client, SocketShutdown.Receive);

                }
                catch (Exception ex)
                {
                    IoHelper.WriteLineWithColor(IoHelper.ERROR, $"Accept failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Data communication between the local client and the lobby server.
        /// </summary>
        /// <param name="client">The client socket</param>
        /// <param name="sharedState">Shared State</param>
        public static void PolDataComm(ref Socket client, SharedState sharedState)
        {
            byte[] byteReceivBuffer = new byte[1024];

            int result = 0, x = 0;
            double t = 0;
            bool isNewChar = false;

            do
            {
                /* Attempt to receive incoming data.. */
                result = client.Receive(byteReceivBuffer, byteReceivBuffer.Length, SocketFlags.None);
                if (result <= 0 && sharedState.IsRunning)
                {
                    // TODO: Append correct error to message
                    IoHelper.WriteLineWithColor(IoHelper.ERROR, $"Client recv failed: ");
                    break;
                }

                char temp = Encoding.ASCII.GetString(byteReceivBuffer).ToCharArray()[0x04];

                // TODO: Replace this total garbage with something better
                // meant to port the following line:
                // //memset(recvBuffer, 0x00, 32);
                byte[] copyFromArray = Enumerable.Repeat((byte)0x00, 32).ToArray();
                Buffer.BlockCopy(copyFromArray, 0, byteReceivBuffer, 0, 32);

                switch (x)
                {
                    case 0:
                        byteReceivBuffer[0] = (byte)0x81;
                        t = (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
                        byteReceivBuffer[0x14] = Convert.ToByte(t);
                        result = 24;
                        break;

                    case 1:
                        if (temp != 0x28)
                        {
                            isNewChar = true;
                        }
                        byteReceivBuffer[0x00] = 0x28;
                        byteReceivBuffer[0x04] = 0x20;
                        byteReceivBuffer[0x08] = 0x01;
                        byteReceivBuffer[0x0B] = 0x7F;
                        result = isNewChar ? 144 : 24;
                        if (isNewChar)
                        {
                            isNewChar = false;
                        }
                        break;
                }

                /* Echo back the buffer to the server.. */
                try
                {
                    client.Send(byteReceivBuffer, result, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    IoHelper.WriteLineWithColor(IoHelper.ERROR, $"Client send failed: {ex.Message}");
                    break;
                }

                /* Increase the current packet count.. */
                x++;
                if (x == 3)
                {
                    break;
                }

            } while (result > 0);
        }

        /// <summary>
        /// Creates a listening server on the given port and protocol.
        /// </summary>
        /// <param name="sock">The socket object to bind to.</param>
        /// <param name="protocol">The protocol to use on the new listening socket.</param>
        /// <param name="port">The port to bind to listen on.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool CreateListenServer(ref Socket sock, ProtocolType protocol, string port)
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList.First(addr => addr.AddressFamily == AddressFamily.InterNetwork);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Convert.ToInt32(port));

            // Create the Listener socket
            sock = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream,
                protocol);

            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

            // Bind to Local address
            try
            {
                sock.Bind(localEndPoint);
            }
            catch (Exception ex)
            {
                // TODO: Add socket error to message
                IoHelper.WriteLineWithColor(IoHelper.ERROR, $"Failed to bind to listening socket: {ex.Message}");
                sock.Close();
                return false;
            }

            if (protocol == ProtocolType.Tcp)
            {
                try
                {
                    sock.Listen((int)SocketOptionName.MaxConnections);

                }
                catch (Exception ex)
                {
                    IoHelper.WriteLineWithColor(IoHelper.ERROR, $"Failed to listen for connections: {ex.Message}");
                    sock.Close();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Lock/Signal a system shutdown by modifying running state.
        /// </summary>
        /// <param name="sharedState">Shared thread state (bool, mutex, ConditionVariable).</param>
        public static void NotifyShutdown(SharedState sharedState)
        {
            if (sharedState.IsRunning)
            {
                // std::lock_guard<std::mutex> lock(sharedState.mutex);
                sharedState.Mutex?.WaitOne(); ;
                sharedState.IsRunning = false;
                //sharedState.ConditionVariable.notify_all();
            }
        }

        /// <summary>
        /// Cleans up a socket via shutdown/close.
        /// </summary>
        /// <param name="sock">Socket reference.</param>
        /// <param name="how">Shutdown send, recv, or both.</param>
        public static void CleanupSocket(Socket sock, SocketShutdown how)
        {
            sock.Shutdown(how);
            sock.Close();
            sock.Dispose();
        }

        /// <summary>
        /// Establishes connection to server
        /// </summary>
        /// <param name="dataSocket">DataSocket with applicable Socket</param>
        /// <param name="server">Server to connect to</param>
        /// <param name="port">Port to connect on</param>
        /// <returns></returns>
        internal static bool CreateConnection(DataSocket dataSocket, string server, string port)
        {
            byte[] bytes = new byte[1024];

            try
            {
                // Create a TCP/IP  socket.    
                Socket sender = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);

                dataSocket.Socket = sender;
                IPHostEntry ipHostInfo = Dns.GetHostEntry(IPAddress.Parse(server));
                IPAddress ipAddress = ipHostInfo.AddressList.First(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                dataSocket.ServerAddress = BitConverter.ToUInt32(ipAddress.GetAddressBytes(), 0);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try
                {
                    sender.Connect(Dns.GetHostAddresses(server), Convert.ToInt32(port));

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return true;
        }
    }
}
