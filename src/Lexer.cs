using System;
using System.Text.RegularExpressions;
using Zenith.Display;
using Zenith.Error;
using Zenith.Logs;

namespace Zenith.Tokenization
{
    /// <summary>
    /// Lexical analyzer that converts preprocessed Taskfile text into a sequence of <see cref="Token"/> instances.
    /// Recognizes task headers, set statements, commands, dependencies and newlines and attaches source line numbers.
    /// </summary>
    public class Lexer
    {
        private readonly List<Token> _tokens = new();

        /// <summary>
        /// Tokenizes the provided preprocessed input into a list of tokens.
        /// The input should have comments and fully blank lines removed prior to calling this method.
        /// </summary>
        /// <param name="input">Preprocessed Taskfile content.</param>
        /// <returns>A list of <see cref="Token"/> objects with correct types and line numbers.</returns>
        public List<Token> Tokenize(string input)
        {
            Logger.Instance.Write("Generating tokens...", LoggerLevel.IGNORE);
            _tokens.Clear();

            if (string.IsNullOrEmpty(input))
            {
                Logger.Instance.WriteError(new TokenError("EOF reached unexpectedly! Is your Taskfile empty?"));
            }

            string[] lines = input.Split(new[] { '\n' }, StringSplitOptions.None);
            bool handledCommands = false;
            int lastCommandLine = 0;
            int lineNumber = 1;

            for (int i = 0; i < lines.Length; i++)
            {
                lineNumber++;
                string rawLine = lines[i];

                if (rawLine.StartsWith("#")) continue;
                if (string.IsNullOrWhiteSpace(rawLine)) continue;

                // Keep the raw indentation to detect command lines
                int indent = CountLeadingTabsOrSpaces(rawLine);

                string line = rawLine.TrimEnd('\r');

                if (string.IsNullOrWhiteSpace(line))
                {
                    _tokens.Add(new Token(TokenType.NEWLINE, string.Empty, lineNumber));
                    continue;
                }

                if (indent > 0)
                {
                    string commandText = line.TrimStart();
                    _tokens.Add(new Token(TokenType.COMMAND, commandText, lineNumber));
                    handledCommands = true;
                    lastCommandLine = lineNumber;
                    continue;
                }

                if (handledCommands)
                {
                    _tokens.Add(new Token(TokenType.NEWLINE, string.Empty, lastCommandLine));
                    handledCommands = false;
                }

                // top-level line (no indentation) is ether a "set", a task header or an identifier
                string trimmed = line.Trim();

                if (trimmed.StartsWith("set ", StringComparison.Ordinal))
                {
                    ParseSetLine(trimmed, lineNumber);
                    _tokens.Add(new Token(TokenType.NEWLINE, string.Empty, lineNumber));
                    continue;
                }

                // Task header: contains ":" after the name portion, Could be "task name: deps" or "name: deps"
                int colonIndex = IndexOfTopLevelColon(trimmed);
                if (colonIndex >= 0)
                {
                    ParseTaskHeader(trimmed, colonIndex, lineNumber);
                    _tokens.Add(new Token(TokenType.NEWLINE, string.Empty, lineNumber));
                    continue;
                }

                // Fallback: treat as identifier / unknown line (e.g., "task build" without colon)
                _tokens.Add(new Token(TokenType.IDENTIFIER, trimmed, lineNumber));
                _tokens.Add(new Token(TokenType.NEWLINE, string.Empty, lineNumber));
            }

            return _tokens;
        }

        #region Helpers

        /// <summary>
        /// Counts leading spaces or tabs to determine indentation level used for command detection.
        /// </summary>
        private static int CountLeadingTabsOrSpaces(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int count = 0;
            while (count < s.Length && (s[count] == ' ' || s[count] == '\t')) count++;
            return count;
        }

        /// <summary>
        /// Finds the index of a colon that is not inside quoted strings (top-level colon).
        /// Returns -1 when no top-level colon is found.
        /// </summary>
        private static int IndexOfTopLevelColon(string line)
        {
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"') inQuotes = !inQuotes;
                if (!inQuotes && ch == ':') return i;
            }
            return -1;
        }

        /// <summary>
        /// Parses a "set NAME = VALUE" top-level line into corresponding tokens. VALUE may be quoted.
        /// </summary>
        private void ParseSetLine(string trimmedLine, int lineNumber)
        {
            // trimmedLine starts with "set "
            // use regex to be robust about spaces: set <NAME> = <VALUE>
            // VALUE may contain spaces and may be quoted.
            var m = Regex.Match(trimmedLine, @"^set\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$");
            if (!m.Success)
            {
                Logger.Instance.WriteError(new SyntaxError($"Unexpected token {trimmedLine}", lineNumber));
            }

            string name = m.Groups[1].Value;
            string rawValue = m.Groups[2].Value.Trim();

            _tokens.Add(new Token(TokenType.KEYWORD_SET, "set", lineNumber, TokenType.KEYWORD));
            _tokens.Add(new Token(TokenType.IDENTIFIER, name, lineNumber));
            _tokens.Add(new Token(TokenType.EQUALS, "=", lineNumber));

            // handle quoted value
            if (rawValue.Length >= 2 && rawValue.StartsWith("\"") && rawValue.EndsWith("\""))
            {
                string inner = rawValue.Substring(1, rawValue.Length - 2);
                _tokens.Add(new Token(TokenType.STRING, inner, lineNumber));
            }
            else
            {
                // unqouted value: take whole remaining string
                _tokens.Add(new Token(TokenType.STRING, rawValue, lineNumber));
            }
        }

        /// <summary>
        /// Parses a task header (left: name, right: comma-separated dependencies) into tokens.
        /// </summary>
        private void ParseTaskHeader(string trimmedLine, int colonIndex, int lineNumber)
        {
            string left = trimmedLine.Substring(0, colonIndex).Trim();
            string right = trimmedLine.Substring(colonIndex + 1).Trim();

            // Left might be "task NAME" or just "NAME"
            if (left.StartsWith("task ", StringComparison.Ordinal))
            {
                _tokens.Add(new Token(TokenType.KEYWORD_TASK, "task", lineNumber, TokenType.KEYWORD));
                string after = left.Substring("task ".Length).Trim();

                if (string.Equals(after, "null"))
                {
                    Logger.Instance.WriteError(new SyntaxError("Invalid task name 'null'", lineNumber));
                }

                _tokens.Add(new Token(TokenType.IDENTIFIER, after, lineNumber));
            }
            else
            {
                // treated left as task name
                _tokens.Add(new Token(TokenType.KEYWORD_TASK, "task", lineNumber, TokenType.KEYWORD));

                if (string.Equals(left, "null"))
                {
                    Logger.Instance.WriteError(new SyntaxError("Invalid task name 'null'", lineNumber));
                }

                _tokens.Add(new Token(TokenType.IDENTIFIER, left, lineNumber));
            }

            _tokens.Add(new Token(TokenType.COLON, ":", lineNumber));

            // Dependencies: comma-separated (may be empty)
            if (!string.IsNullOrEmpty(right))
            {
                // split by commas that are not inside quotes
                var deps = SplitDependencies(right);
                bool first = true;
                foreach (var dep in deps)
                {
                    if (!first)
                    {
                        _tokens.Add(new Token(TokenType.DEPENDENCY, ",", lineNumber));
                    }
                    _tokens.Add(new Token(TokenType.DEPENDENCY, dep, lineNumber));
                    first = false;
                }
            }
            else
            {
                Logger.Instance.WriteError(new SyntaxError("Dependencies cannot be empty", lineNumber));
            }
        }

        /// <summary>
        /// Splits a comma-separated dependency list while respecting quoted dependency names.
        /// </summary>
        private static IEnumerable<string> SplitDependencies(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                yield break;
            }

            int start = 0;
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];

                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (ch == ',' && !inQuotes)
                {
                    string candidate = input.Substring(start, i - start).Trim();
                    if (candidate.Length > 0)
                    {
                        yield return candidate;
                    }
                    start = i + 1;
                }
            }

            string tail = input.Substring(start).Trim();
            if (tail.Length > 0)
            {
                yield return tail;
            }
        }

        #endregion

        /// <summary>
        /// Prints debugging information for a list of tokens using the <see cref="Output"/> helper.
        /// </summary>
        /// <param name="tokens">Tokens to print.</param>
        public void PrintTokens(List<Token> tokens)
        {
            Output.DisplayDebug("===== Tokens =====");
            foreach (Token token in tokens)
            {
                token.PrintToken();
            }
        }
    }

    /// <summary>
    /// Represents a lexical token emitted by the <see cref="Lexer"/>.
    /// </summary>
    public class Token
    {
        /// <summary>
        /// The general category of the token when applicable (defaults to <see cref="TokenType.UNKNOWN"/>).
        /// </summary>
        public TokenType GeneralType { get; set; } = TokenType.UNKNOWN;

        /// <summary>
        /// The concrete token type (e.g. <see cref="TokenType.IDENTIFIER"/> or <see cref="TokenType.STRING"/>).
        /// </summary>
        public TokenType Type { get; set; }

        /// <summary>
        /// The textual value of the token (without surrounding quotes for strings).
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// The 1-based line number in the source Taskfile where the token was found.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Creates a new <see cref="Token"/> instance.
        /// </summary>
        public Token(TokenType type, string value, int lineNumber, TokenType generalType = TokenType.UNKNOWN)
        {
            Type = type;
            Value = value;
            LineNumber = lineNumber;
            GeneralType = generalType;
        }

        /// <summary>
        /// Writes a debug representation of this token to the configured output.
        /// </summary>
        public void PrintToken()
        {
            Output.DisplayDebug($"Type: {Type}");
            Output.DisplayDebug($"Value: {Value}");
            Output.DisplayDebug($"Line: {LineNumber}");
            Output.DisplayDebug($"General Type: {GeneralType}");
            Output.DisplayDebug("==================");
        }
    }

    /// <summary>
    /// All token types produced by the lexer.
    /// </summary>
    public enum TokenType
    {
        KEYWORD, KEYWORD_SET, KEYWORD_TASK, IDENTIFIER, EQUALS, STRING,
        DEPENDENCY, COMMAND, NEWLINE, COLON, UNKNOWN
    }
}
