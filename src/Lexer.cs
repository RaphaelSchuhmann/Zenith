using System;
using System.Text.RegularExpressions;
using Zenith.Error;

namespace Zenith.Tokenization
{
    public class Lexer
    {
        private readonly List<Token> _tokens = new();

        /// <summary>
        /// Main entry: takes preprocessed file content (no blank lines, no comments)
        /// and returns a list of tokens with correct line numbers.
        /// </summary>
        public List<Token> Tokenize(string input)
        {
            _tokens.Clear();

            if (string.IsNullOrEmpty(input))
            {
                throw new TokenError("EOF reached unexpectedly! Is your Taskfile empty?");
            }

            string[] lines = input.Split(new[] { '\n' }, StringSplitOptions.None);
            bool handledCommands = false;
            int lastCommandLine = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNumber = i + 1;
                string rawLine = lines[i];

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

        private static int CountLeadingTabsOrSpaces(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int count = 0;
            while (count < s.Length && (s[count] == ' ' || s[count] == '\t')) count++;
            return count;
        }

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

        // Parse "set NAME = VALUE" with optional quoting for VALUE 
        private void ParseSetLine(string trimmedLine, int lineNumber)
        {
            // trimmedLine starts with "set "
            // use regex to be robust about spaces: set <NAME> = <VALUE>
            // VALUE may contain spaces and may be quoted.
            var m = Regex.Match(trimmedLine, @"^set\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$");
            if (!m.Success)
            {
                throw new SyntaxError($"Unexpected token {trimmedLine}", lineNumber);
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

        // Parse a task header at colonIndex: left part is a name (maybe "task name"), right part is dependencies
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
                    throw new SyntaxError("Invalid task name 'null'", lineNumber);
                }

                _tokens.Add(new Token(TokenType.IDENTIFIER, after, lineNumber));
            }
            else
            {
                // treated left as task name
                _tokens.Add(new Token(TokenType.KEYWORD_TASK, "task", lineNumber, TokenType.KEYWORD));

                if (string.Equals(left, "null"))
                {
                    throw new SyntaxError("Invalid task name 'null'", lineNumber);
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
                throw new SyntaxError("Dependencies cannot be empty", lineNumber);
            }
        }

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

        public void PrintTokens(List<Token> tokens)
        {
            foreach (Token token in tokens)
            {
                token.PrintToken();
            }
        }
    }

    public class Token
    {
        public TokenType GeneralType { get; set; } = TokenType.UNKNOWN;
        public TokenType Type { get; set; }
        public string Value { get; set; } = string.Empty;
        public int LineNumber { get; set; }

        public Token(TokenType type, string value, int lineNumber, TokenType generalType = TokenType.UNKNOWN)
        {
            Type = type;
            Value = value;
            LineNumber = lineNumber;
            GeneralType = generalType;
        }

        public void PrintToken()
        {
            Console.WriteLine($"Type: {Type}");
            Console.WriteLine($"Value: {Value}");
            Console.WriteLine($"Line: {LineNumber}");
            Console.WriteLine($"General Type: {GeneralType}");
            Console.WriteLine("---------------------------");
        }
    }

    public enum TokenType
    {
        KEYWORD, KEYWORD_SET, KEYWORD_TASK, IDENTIFIER, EQUALS, STRING,
        DEPENDENCY, COMMAND, NEWLINE, COLON, UNKNOWN
    }
}
