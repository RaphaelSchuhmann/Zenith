using System;
using Zenith.Display;
using Zenith.Error;
using Zenith.Logs;
using Zenith.Models;
using Zenith.Tokenization;

namespace Zenith.Parse
{
    /// <summary>
    /// Parses a sequence of <see cref="Token"/> instances produced by the lexer into a <see cref="TaskfileModel"/>.
    /// Responsible for grouping tokens into logical blocks (variables and tasks) and delegating to specific parsers.
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// Parses the provided token list into a strongly-typed <see cref="TaskfileModel"/> containing variables and tasks.
        /// Throws or logs an internal error when tokens are malformed or missing.
        /// </summary>
        /// <param name="tokens">The list of tokens emitted by the lexer.</param>
        /// <returns>A populated <see cref="TaskfileModel"/> representing the Taskfile.</returns>
        public TaskfileModel Parse(List<Token> tokens)
        {
            Logger.Instance.Write("Parsing tokens...", LoggerLevel.IGNORE);
            TaskfileModel taskFile = new TaskfileModel();

            // Check if list is empty
            if (!tokens.Any())
            {
                Logger.Instance.WriteError(new Internal("Parser encountered an empty list of tokens!"));
            }

            // Group tokens
            // The actual token type is irrelevant for this search
            List<int> keywordIndexes = GetTokenIndexes(tokens, new Token(TokenType.NEWLINE, string.Empty, 0, TokenType.KEYWORD), true);

            List<TokenGroup> tokenGroups = new();

            try
            {
                for (int i = 0; i < keywordIndexes.Count; i++)
                {
                    int start = keywordIndexes[i];

                    if (i + 1 != keywordIndexes.Count)
                    {
                        int end = keywordIndexes[i + 1];
                        tokenGroups.Add(new TokenGroup(tokens[start..end], tokens[keywordIndexes[i]].Type));
                    }
                    else
                    {
                        tokenGroups.Add(new TokenGroup(tokens[keywordIndexes[i]..], tokens[keywordIndexes[i]].Type));
                    }
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.Instance.WriteError(new Internal($"Invalid operation in parser: {ex.Message}"));
            }

            foreach (TokenGroup group in tokenGroups)
            {
                if (group.Type == TokenType.KEYWORD_SET)
                {
                    taskFile.Variables.Add(ParseVariableGroup(group));
                }
                else if (group.Type == TokenType.KEYWORD_TASK)
                {
                    taskFile.Tasks.Add(ParseTaskGroup(group));
                }
                else
                {
                    Logger.Instance.WriteError(new SyntaxError($"Unexpected token {group.Group[0].Value}", group.Group[0].LineNumber));
                }
            }

            return taskFile;
        }

        #region Helpers

        /// <summary>
        /// Returns the indexes of tokens in the provided list that match the searched token type.
        /// When <paramref name="filterGeneralType"/> is true the GeneralType property is compared instead of Type.
        /// </summary>
        private static List<int> GetTokenIndexes(List<Token> tokens, Token searchedToken, bool filterGeneralType = false)
        {
            List<int> indexes = new();

            for (int i = 0; i < tokens.Count; i++)
            {
                if (!filterGeneralType)
                {
                    if (tokens[i].Type == searchedToken.Type)
                    {
                        indexes.Add(i);
                    }
                }
                else
                {
                    if (tokens[i].GeneralType == searchedToken.GeneralType)
                    {
                        indexes.Add(i);
                    }
                }
            }

            return indexes;
        }

        /// <summary>
        /// Parses a token group representing a variable declaration into a <see cref="VariableModel"/>.
        /// Expects the token group to follow the pattern: KEYWORD_SET, IDENTIFIER, EQUALS, STRING, NEWLINE...
        /// </summary>
        private static VariableModel ParseVariableGroup(TokenGroup tokenGroup)
        {
            VariableModel variable = new VariableModel();

            if (tokenGroup.Group.Count <= 1)
            {
                // Note that tokenGroup here can never be empty since there has to be at least one token to call this function
                Logger.Instance.WriteError(new SyntaxError("Incomplete variable declaration", tokenGroup.Group[0].LineNumber));
            }

            variable.Name = tokenGroup.Group[1].Value;
            variable.LineNumber = tokenGroup.Group[0].LineNumber;
            variable.Value = tokenGroup.Group[3].Value;

            return variable;
        }

        /// <summary>
        /// Parses a token group representing a task declaration into a <see cref="TaskModel"/>.
        /// Extracts task name, dependencies, commands and associated line numbers. Validation errors will be logged.
        /// </summary>
        private static TaskModel ParseTaskGroup(TokenGroup tokenGroup)
        {
            TaskModel task = new TaskModel();

            if (tokenGroup.Group.Count <= 1)
            {
                // Note that tokenGroup here can never be empty since there has to be at least one token to call this function
                Logger.Instance.WriteError(new SyntaxError("Incomplete task declaration", tokenGroup.Group[0].LineNumber));
            }

            task.Name = tokenGroup.Group[1].Value;
            task.LineNumber = tokenGroup.Group[0].LineNumber;

            List<int> indexesNewLine = GetTokenIndexes(tokenGroup.Group, new Token(TokenType.NEWLINE, string.Empty, 0));

            List<Token> dependencies = tokenGroup.Group[3..indexesNewLine[0]];
            foreach (Token dep in dependencies)
            {
                if (dep.Value != ",")
                {
                    if (dep.Value != "")
                    {
                        task.Dependencies.Add(dep.Value);
                    }
                    else
                    {
                        Logger.Instance.WriteError(new SyntaxError("Invalid task declaration, dependencies can not be empty!\nUse 'null' if you don't want any dependencies", task.LineNumber));
                    }
                }
            }

            int commandsStart = indexesNewLine[0] + 1;
            int commandsEnd = indexesNewLine.Count > 1 ? indexesNewLine[1] : tokenGroup.Group.Count;
            if (commandsStart >= commandsEnd)
            {
                Logger.Instance.WriteError(new SyntaxError("Invalid task declaration, commands can not be empty", task.LineNumber));
            }
            List<Token> commands = tokenGroup.Group[commandsStart..commandsEnd];
            foreach (Token cmd in commands)
            {
                if (cmd.Value != "")
                {
                    task.Commands.Add(cmd.Value);
                }
                else
                {
                    Logger.Instance.WriteError(new SyntaxError("Invalid task declaration, commands can not be empty", task.LineNumber));
                }
            }

            return task;
        }

        #endregion
    }

    /// <summary>
    /// Represents a contiguous group of tokens that belong together (for example a variable declaration or a task block).
    /// </summary>
    class TokenGroup
    {
        /// <summary>
        /// The tokens that make up this group.
        /// </summary>
        public List<Token> Group { get; set; } = new();

        /// <summary>
        /// The primary token type of the group (e.g. <see cref="TokenType.KEYWORD_SET"/> or <see cref="TokenType.KEYWORD_TASK"/>).
        /// </summary>
        public TokenType Type { get; set; }

        /// <summary>
        /// Creates a new token group for the provided tokens and type.
        /// </summary>
        public TokenGroup(List<Token> group, TokenType type)
        {
            Group = group;
            Type = type;
        }

        /// <summary>
        /// Prints a debug representation of the group and its tokens to the configured output.
        /// </summary>
        public void PrintGroup()
        {
            Output.DisplayDebug("===== Printing Group =====");
            Output.DisplayDebug($"Type: {Type}");
            Output.DisplayDebug("Tokens: ");
            foreach (Token token in Group)
            {
                token.PrintToken();
            }
        }
    }
}
