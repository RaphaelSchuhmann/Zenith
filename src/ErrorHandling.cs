using System;

namespace Zenith.Error
{
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

    public sealed class Internal : ZenithException
    {
        public Internal(string message) : base($"Internal error: {message}") { }
    }

    public sealed class Unknown : ZenithException
    {
        public Unknown() : base("Unknown error") { }
    }
}
