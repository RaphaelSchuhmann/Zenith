using System;

namespace Zenith.Error
{
    /// <summary>
    /// Handles formatting and output of errors for the CLI and terminates the process with a non-zero exit code.
    /// </summary>
    public class ErrorReporter
    {
        /// <summary>
        /// Displays the provided <see cref="ZenithException"/> to standard error with color highlighting.
        /// If the exception contains an inner IO exception, its type is printed as the cause.
        /// This method will terminate the process with exit code 1.
        /// </summary>
        /// <param name="ex">The exception to display.</param>
        public static void DisplayError(ZenithException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            
            Console.Error.Write($"{ex.Message}\n");

            if (ex is IoError ioe && ioe.InnerException != null)
            {
                Console.Error.WriteLine($"Cause: {ioe.InnerException.GetType().Name} Error");
            }

            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Base exception type for errors produced by Zenith.
    /// Contains an optional <see cref="Line"/> property indicating the source line related to the error.
    /// </summary>
    public class ZenithException : Exception
    {
        /// <summary>
        /// The source line related to the error, or 0 when not applicable.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZenithException"/> class with the specified message
        /// and an optional line number.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="line">The line number associated with the error.</param>
        public ZenithException(string message, int line = 0) : base(message)
        {
            Line = line;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZenithException"/> class with a message, inner exception
        /// and an optional line number.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The underlying exception that caused this error.</param>
        /// <param name="line">The line number associated with the error.</param>
        public ZenithException(string message, Exception innerException, int line = 0)
            : base(message, innerException)
        {
            Line = line;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZenithException"/> class without additional information.
        /// </summary>
        public ZenithException() : base()
        {
            Line = 0;
        }
    }

    /// <summary>
    /// Represents an error that occurred during tokenization of the Taskfile.
    /// </summary>
    public sealed class TokenError : ZenithException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TokenError"/> with the specified message.
        /// </summary>
        /// <param name="message">A description of the tokenization error.</param>
        public TokenError(string message) : base($"Token error: {message}") { }
    }

    /// <summary>
    /// Represents a syntax error encountered while parsing the Taskfile.
    /// </summary>
    public sealed class SyntaxError : ZenithException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SyntaxError"/> with the specified message and line number.
        /// </summary>
        /// <param name="message">A description of the syntax error.</param>
        /// <param name="line">The line number where the error occurred.</param>
        public SyntaxError(string message, int line) : base($"Syntax error: {message} at line: {line}") { }
    }

    /// <summary>
    /// Represents an IO-related error, wrapping an underlying <see cref="Exception"/>.
    /// </summary>
    public sealed class IoError : ZenithException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IoError"/> that wraps an inner exception.
        /// </summary>
        /// <param name="message">A description of the IO error.</param>
        /// <param name="inner">The underlying exception that caused the IO error.</param>
        public IoError(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Represents an error caused by invalid user input.
    /// </summary>
    public sealed class UserInputError : ZenithException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UserInputError"/> with the specified message.
        /// </summary>
        /// <param name="message">A description of the invalid input.</param>
        public UserInputError(string message) : base($"Invalid input: {message}") { }
    }

    /// <summary>
    /// Represents a failure when executing an external command or task action.
    /// </summary>
    public sealed class CommandError : ZenithException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CommandError"/> with the specified message.
        /// </summary>
        /// <param name="message">A description of the command failure.</param>
        public CommandError(string message) : base($"Command failed: {message}") { }
    }

    /// <summary>
    /// Represents an internal error in the application.
    /// </summary>
    public sealed class Internal : ZenithException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Internal"/> with the specified message.
        /// </summary>
        /// <param name="message">A description of the internal error.</param>
        public Internal(string message) : base($"Internal error: {message}") { }
    }

    /// <summary>
    /// Represents an unknown error scenario.
    /// </summary>
    public sealed class Unknown : ZenithException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Unknown"/>.
        /// </summary>
        public Unknown() : base("Unknown error") { }
    }
}
