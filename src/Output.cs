using System;
using Zenith.Error;

namespace Zenith.Display
{
    /// <summary>
    /// Helper methods for printing colored and prefixed output to the console.
    /// Provides convenience methods for informational, warning, error, success and debug messages.
    /// </summary>
    public class Output
    {
        /// <summary>
        /// Writes an informational message prefixed with [INFO] in blue color.
        /// </summary>
        public static void DisplayInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("[INFO] ");
            Console.ResetColor();

            Console.Write($"{message}\n");
        }

        /// <summary>
        /// Writes a warning message prefixed with [WARNING] in yellow color.
        /// </summary>
        public static void DisplayWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[WARNING] ");
            Console.ResetColor();

            Console.Write($"{message}\n");
        }

        /// <summary>
        /// Displays an error prefix and delegates to <see cref="ErrorReporter"/> which will handle the error display and process termination.
        /// </summary>
        /// <param name="ex">The exception to display.</param>
        public static void DisplayError(ZenithException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ERROR] ");
            Console.ResetColor();

            ErrorReporter.DisplayError(ex);
        }

        /// <summary>
        /// Writes a success message prefixed with [SUCCESS] in green color.
        /// </summary>
        public static void DisplaySuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[SUCCESS] ");
            Console.ResetColor();

            Console.Write($"{message}\n");
        }

        /// <summary>
        /// Writes a debug message prefixed with [DEBUG] in magenta color.
        /// </summary>
        /// <param name="message">The debug text to display.</param>
        public static void DisplayDebug(string message)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("[DEBUG] ");
            Console.ResetColor();

            Console.Write($"{message}\n");
        }
    }
}
