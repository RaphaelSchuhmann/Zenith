using System;
using Zenith.Error;

namespace Zenith.Display
{
    public class Output
    {
        public static void DisplayInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("[INFO] ");
            Console.ResetColor();

            Console.Write($"{message}\n");
        }

        public static void DisplayWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[WARNING] ");
            Console.ResetColor();

            Console.Write($"{message}\n");
        }

        public static void DisplayError(ZenithException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ERROR] ");
            Console.ResetColor();

            ErrorReporter.DisplayError(ex);
        }

        public static void DisplaySuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[SUCCESS] ");
            Console.ResetColor();

            Console.Write($"{message}\n");
        }

        public static void DisplayDebug(string message)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("[DEBUG] ");
            Console.ResetColor();

            Console.Write($"{message}\n");
        }
    }
}
