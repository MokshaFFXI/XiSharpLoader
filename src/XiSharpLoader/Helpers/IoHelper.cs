using System;
using System.Security;

namespace XiSharpLoader.Helpers
{
    internal static class IoHelper
    {
        internal const ConsoleColor NORMAL = ConsoleColor.Gray;
        internal const ConsoleColor WARNING = ConsoleColor.Yellow;
        internal const ConsoleColor ERROR = ConsoleColor.Red;
        internal const ConsoleColor SUCCESS = ConsoleColor.Green;

        /// <summary>
        /// Display the Banner
        /// </summary>
        public static void DisplayBanner()
        {
            WriteLineWithColor(ConsoleColor.Red, "==========================================================");
            WriteLineWithColor(ConsoleColor.Green, "XiSharpLoader (c) 2021 Malkavius");
            WriteLineWithColor(ConsoleColor.Green, "This program comes with ABSOLUTELY NO WARRANTY.");
            WriteLineWithColor(ConsoleColor.Green, "This is free software; see LICENSE file for details.");
            WriteLineWithColor(ConsoleColor.Blue, "Git Repo   : https://github.com/MokshaFFXI/XiSharpLoader");
            WriteLineWithColor(ConsoleColor.Red, "==========================================================");
        }

        /// <summary>
        /// Display the Menu
        /// </summary>
        public static void DisplayMainMenu()
        {
            WriteLineWithColor(ConsoleColor.Gray, "==========================================================");
            WriteLineWithColor(ConsoleColor.Gray, "What would you like to do?");
            WriteLineWithColor(ConsoleColor.Gray, "   1.) Login");
            WriteLineWithColor(ConsoleColor.Gray, "   2.) Create New Account");
            WriteLineWithColor(ConsoleColor.Gray, "   3.) Change Account Password");
            WriteLineWithColor(ConsoleColor.Gray, "==========================================================");
        }

        /// <summary>
        /// Print messages in specified color
        /// </summary>
        /// <param name="color">Color of message</param>
        /// <param name="message">Message text</param>
        public static void WriteLineWithColor(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Used to get masked input passwords from the Console
        /// </summary>
        /// <param name="displayMessage">The prompt to enter the password</param>
        /// <returns>Plaintext password</returns>
        public static string GetPasswordFromConsole(string displayMessage)
        {
            SecureString pass = new SecureString();
            Console.Write(displayMessage);
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (!char.IsControl(key.KeyChar))
                {
                    pass.AppendChar(key.KeyChar);
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass.RemoveAt(pass.Length - 1);
                        Console.Write("\b \b");
                    }
                }
            }

            while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();

            return new System.Net.NetworkCredential(string.Empty, pass).Password;
        }
    }
}
