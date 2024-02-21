using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nyt_crossword_downloader
{

    /// <summary>
    /// Static methods.
    /// </summary>
    internal static class Methods
    {
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }

        public static void LogError(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {message}");
            Console.ForegroundColor = prevColor;
        }
    }
}
