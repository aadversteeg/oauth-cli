using Core.Application;
using System;

namespace Core.Infrastructure.ConsoleApp
{
    public class PasswordProvider : IPasswordProvider
    {
        public string GetPassword(string prompt)
        {
            var password = string.Empty;

            Console.Write(prompt);
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey(true);
                // Skip if Backspace or Enter is Pressed
                if (keyInfo.Key != ConsoleKey.Backspace && keyInfo.Key != ConsoleKey.Enter)
                {
                    password += keyInfo.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (keyInfo.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        // Remove last charcter if Backspace is Pressed
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                }
            }
            // Stops Getting Password Once Enter is Pressed
            while (keyInfo.Key != ConsoleKey.Enter);

            return password;
        }
    }
}
