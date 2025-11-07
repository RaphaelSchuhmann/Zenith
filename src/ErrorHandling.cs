using System;

namespace Zenith.Error
{
    public class ErrorReporter
    {
        public static void DisplayError(ZenithException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            Console.Error.WriteLine("[--- FATAL ZENITH ERROR ---]");
            Console.Error.WriteLine(ex.Message);

            if (ex is IoError ioe && ioe.InnerException != null)
            {
                Console.Error.WriteLine($"Cause: {ioe.InnerException.GetType().Name} Error");
            }

            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    public class ZenithException : Exception
    {
        public int Line { get; }

        public ZenithException(string message, int line = 0) : base(message)
        {
            Line = line;
        }

        public ZenithException(string message, Exception innerException, int line = 0)
            : base(message, innerException)
        {
            Line = line;
        }

        public ZenithException() : base()
        {
            Line = 0;
        }
    }

    public sealed class TokenError : ZenithException
    {
        public TokenError(string message) : base($"Token error: {message}") { }
    }

    public sealed class SyntaxError : ZenithException
    {
        public SyntaxError(string message, int line) : base($"Syntax error: {message} at line: {line}") { }
    }

    public sealed class IoError : ZenithException
    {
        public IoError(string message, Exception inner) : base(message, inner) { }
    }

    public sealed class UserInputError : ZenithException
    {
        public UserInputError(string message) : base($"Invalid input: {message}") { }
    }

    public sealed class CommandError : ZenithException
    {
        public CommandError(string message) : base($"Command failed: {message}") { }
    }

    public sealed class Internal : ZenithException
    {
        public Internal(string message) : base($"Internal error: {message}") { }
    }

    public sealed class Unknown : ZenithException
    {
        public Unknown() : base("Unknown error") { }
    }
}
