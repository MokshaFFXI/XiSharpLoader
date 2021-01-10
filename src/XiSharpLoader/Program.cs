using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using XiSharpLoader.Extensions;
using XiSharpLoader.Helpers;
using XiSharpLoader.Hook;
using XiSharpLoader.Models;

using static XiSharpLoader.Helpers.Enums;

namespace XiSharpLoader
{
    class Program
    {
        private static bool _hide = false;
        private static string _serverAddress;
        public const int LOBBY_SERVER_PORT = 51220;

        /// <summary>
        /// Main program entry point
        /// </summary>
        /// <param name="server">The servers IP Address / Domain Name</param>
        /// <param name="port">The server's port number</param>
        /// <param name="user">Username</param>
        /// <param name="pass">Password</param>
        /// <param name="lang">Language Code</param>
        /// <param name="hairpin">Use Hairpin fix</param>
        /// <param name="hide"></param>
        static void Main(string server = "127.0.0.1", string port = "51220", string user = "", string pass = "", string lang = "", bool hairpin = false, bool hide = false)
        {
            bool useHairpinFix = hairpin;
            _serverAddress = server;
            PolLanguage language = lang.ToPolLanguage();
            string lobbyServerPort = port;
            string username = user;
            string password = pass;
            //char* characterList = NULL; // Pointer to the character list data being sent from the server.characterList
            char[] characterList = null;
            var sharedState = new SharedState(); // shared thread state
            _hide = hide;

            IoHelper.DisplayBanner();

            DataSocket dataSocket = new DataSocket();
            Socket polSocket = null;
            Socket polClientSocket = null;

            if (Network.Network.CreateConnection(dataSocket, _serverAddress, "54231"))
            {
                while (!VerifyAccount(dataSocket, _serverAddress, username, password))
                {
                    Thread.Sleep(10);
                }

                sharedState.IsRunning = true;
                Thread ffxiServer = new Thread(() => Network.Network.FFXiDataComm(dataSocket, _serverAddress, characterList, sharedState));
                ffxiServer.Start();
                Thread polServer = new Thread(() => Network.Network.PolServer(polSocket, polClientSocket, lobbyServerPort, sharedState));
                polServer.Start();
                Thread ffxi = new Thread(() => LaunchFfxi(useHairpinFix, language, characterList, sharedState));
                ffxi.Start();
            }
        }

        /// <summary>
        /// Verifies the players login information; also handles creating new accounts.
        /// </summary>
        /// <param name="dataSocket">The datasocket object with the connection socket.</param>
        /// <param name="server">Server address to connect.</param>
        /// <param name="username">Account username.</param>
        /// <param name="password">Account password.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool VerifyAccount(DataSocket dataSocket, string server, string username, string password)
        {
            bool canAutoLogin = true;

            // TODO: Refactor these into byte[]s
            char[] recvBuffer = new char[1024];
            char[] sendBuffer = new char[1024];

            /* Create connection if required.. */
            if (dataSocket.Socket == null || !dataSocket.Socket.Connected)
            {
                if (!Network.Network.CreateConnection(dataSocket, server, "54231"))
                {
                    return false;
                }
            }

            /* Determine if we should auto-login.. */
            bool bUseAutoLogin = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password) && canAutoLogin;
            if (bUseAutoLogin)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Autologin activated!");
            }

            if (!bUseAutoLogin)
            {
                canAutoLogin = false;

                IoHelper.DisplayMainMenu();

                Console.Write("\nEnter a selection: ");
                string input = Console.ReadLine();

                /* User wants to log into an existing account or modify an existing account's password. */
                if (input == "1" || input == "3")
                {
                    if (input == "3")
                    {
                        Console.WriteLine("Before resetting your password, first verify your account details.");
                    }
                    Console.WriteLine("Please enter your login information.");
                    Console.Write("\nUsername: ");
                    username = Console.ReadLine();

                    // Convert SecureString back to plaintext
                    password = IoHelper.GetPasswordFromConsole("Password: ");

                    char eventCode = (input == "1") ? (char)0x10 : (char)0x30;
                    sendBuffer[0x20] = eventCode;
                }

                /* User wants to create a new account.. */
                else if (input == "2")
                {
                create_account:
                    Console.WriteLine("Please enter your desired login information.");
                    Console.Write("\nUsername (3-15 characters): ");
                    username = Console.ReadLine();

                    password = IoHelper.GetPasswordFromConsole("Password (6-15 characters): ");
                    string verifyPassword = IoHelper.GetPasswordFromConsole("Repeat Password           : ");

                    if (verifyPassword != password)
                    {
                        Console.WriteLine("\nPasswords did not match! Please try again.");
                        goto create_account;
                    }

                    sendBuffer[0x20] = '\x20';
                }

                Console.WriteLine();
            }
            else
            {
                sendBuffer[0x20] = '\x10';
                canAutoLogin = false;
            }

            /* Copy username and password into buffer.. */
            username.CopyTo(0, sendBuffer, 0x00, username.Length);
            password.CopyTo(0, sendBuffer, 0x10, password.Length);

            /* Send info to server and obtain response.. */
            dataSocket.Socket.Send(Encoding.ASCII.GetBytes(sendBuffer), 33, SocketFlags.None);
            byte[] recieveBuffer = new byte[1024];
            dataSocket.Socket.Receive(recieveBuffer, 16, SocketFlags.None);

            recvBuffer = Encoding.ASCII.GetString(recieveBuffer).ToCharArray();

            switch ((AccountResult)Encoding.ASCII.GetString(recieveBuffer).ToCharArray()[0])
            {
                case AccountResult.Login_Success: // 0x001                    
                    IoHelper.WriteLineWithColor(IoHelper.SUCCESS, $"Successfully logged in as {username}!");
                    dataSocket.AccountId = BitConverter.ToUInt32(recieveBuffer, 0x01);
                    dataSocket.Socket.Close();
                    dataSocket.Socket.Dispose();
                    return true;

                case AccountResult.Login_Error: // 0x002                    
                    IoHelper.WriteLineWithColor(IoHelper.ERROR, "Failed to login. Invalid username or password.");
                    dataSocket.Socket.Close();
                    dataSocket.Socket.Dispose();
                    return false;

                case AccountResult.Create_Success: // 0x003                    
                    IoHelper.WriteLineWithColor(IoHelper.SUCCESS, "Account successfully created!");
                    dataSocket.Socket.Close();
                    dataSocket.Socket.Dispose();
                    canAutoLogin = true;
                    return false;

                case AccountResult.Create_Taken: // 0x004                    
                    IoHelper.WriteLineWithColor(IoHelper.ERROR, "Failed to create account. Username already taken.");
                    dataSocket.Socket.Close();
                    dataSocket.Socket.Dispose();
                    return false;

                //TODO: Test this?
                case AccountResult.Create_Disabled: // 0x008                    
                    IoHelper.WriteLineWithColor(IoHelper.ERROR, "Failed to create account. This server does not allow account creation through the loader.");
                    dataSocket.Socket.Close();
                    dataSocket.Socket.Dispose();
                    return false;

                //TODO: Test this?
                case AccountResult.Create_Error: // 0x009                    
                    IoHelper.WriteLineWithColor(IoHelper.ERROR, "Failed to created account, a server-side error occurred.");
                    dataSocket.Socket.Close();
                    dataSocket.Socket.Dispose();
                    return false;

                case AccountResult.PassChange_Request: // Request for updated password to change to.                    
                    IoHelper.WriteLineWithColor(IoHelper.SUCCESS, $"Log in verified for user {username}.");
                    string confirmedPassword;

                    do
                    {
                        password = IoHelper.GetPasswordFromConsole("Enter new password (6-15 characters): ");
                        confirmedPassword = IoHelper.GetPasswordFromConsole("Repeat Password           : ");
                        Console.WriteLine();

                        if (!string.Equals(password, confirmedPassword))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Passwords did not match! Please try again.");
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                    } while (!string.Equals(password, confirmedPassword));

                    /* Clear the buffers */
                    Array.Clear(sendBuffer, 0, 33);
                    Array.Clear(recvBuffer, 0, 16);

                    /* Copy the new password into the buffer. */
                    password.CopyTo(0, sendBuffer, 0, password.Length);

                    /* Send info to server and obtain response.. */
                    dataSocket.Socket.Send(Encoding.ASCII.GetBytes(sendBuffer), 16, SocketFlags.None);
                    byte[] byteRecieveBuffer = new byte[1024];
                    dataSocket.Socket.Receive(byteRecieveBuffer, 16, SocketFlags.None);

                    /* Handle the final result. */
                    switch ((AccountResult)(Encoding.ASCII.GetString(byteRecieveBuffer).ToCharArray()[0]))
                    {
                        case AccountResult.PassChange_Success: // Success (Changed Password)
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Password updated successfully!");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            //password.clear();
                            dataSocket.Socket.Close();
                            dataSocket.Socket.Dispose();
                            return false;

                        case AccountResult.PassChange_Error: // Error (Changed Password)
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to change password.");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            //password.clear();
                            dataSocket.Socket.Close();
                            dataSocket.Socket.Dispose();
                            return false;
                    }

                    break;
            }

            dataSocket.Socket.Close();
            dataSocket.Socket.Dispose();
            return false;
        }

        /// <summary>
        /// Launches POL Core / FFXI and setup Hairpin/Detours.
        /// </summary>
        /// <param name="useHairpinFix">Apply Hairpin fix modification.</param>
        /// <param name="language">POL language.</param>
        /// <param name="characterList">Pointer to character list in memory.</param>
        /// <param name="sharedState">Shared thread state (bool, mutex, condition_variable).</param>
        public static void LaunchFfxi(bool useHairpinFix, PolLanguage language, char[] characterList, SharedState sharedState)
        {
            bool errorState = false;

            /* Initialize COM */
            int hResult = CoInitialize((IntPtr)null);
            //if (hResult != 0x0 && hResult != 0x1)
            //{
            //    IoHelper.WriteLineWithColor(IoHelper.ERROR, $"Failed to initialize COM, error code: {hResult}");
            //    errorState = true;
            //}

            if (!errorState)
            {

                // hook.Dispose() will uninstall the hook for us
                /* Attach detour for gethostbyname.. */
                //DetourTransactionBegin();
                //DetourUpdateThread(GetCurrentThread());                
                //DetourAttach(&(PVOID &)Real_gethostbyname, Mine_gethostbyname);
                //if (DetourTransactionCommit() != NO_ERROR)
                //{
                //    xiloader::console::output(xiloader::color::error, "Failed to detour function 'gethostbyname'. Cannot continue!");
                //    errorState = true;
                //}
            }

            /* Start hairpin hack thread if required.. */
            Thread hairpinfix;
            if (!errorState && useHairpinFix)
            {
                hairpinfix = new Thread(() => ApplyHairpinFix(sharedState));
                hairpinfix.Start();
            }
        }

        private static void ApplyHairpinFix(SharedState sharedState)
        {
            do
            {
                /* Sleep until we find FFXiMain loaded.. */
                Thread.Sleep(100);
            } while (GetModuleHandleA("FFXiMain.dll") == null && sharedState.IsRunning);

            /* Convert server address.. */
            //ResolveHostname(g_ServerAddress.c_str(), &g_NewServerAddress);
        }

        [DllImport("ole32.dll")]
        private static extern int CoInitialize(IntPtr pvReserved);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandleA(string lpModuleName);
    }
}
