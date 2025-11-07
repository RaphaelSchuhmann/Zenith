using System;
using Zenith.Display;
using Zenith.Error;
using Zenith.Models;
using Zenith.Tokenization;

namespace Zenith.Parse
{
    public class Parser
    {
        public TaskfileModel Parse(List<Token> tokens)
        {
            TaskfileModel taskFile = new TaskfileModel();

            // Check if list is empty
            if (!tokens.Any())
            {
                Output.DisplayError(new Internal("Parser encountered an empty list of tokens!"));
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
                Output.DisplayError(new Internal($"Invalid operation in parser: {ex.Message}"));
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
                    Output.DisplayError(new SyntaxError($"Unexpected token {group.Group[0].Value}", group.Group[0].LineNumber));
                }
            }

            return taskFile;
        }

        #region Helpers

        // Note that line number here is irrelevant as it is not checked for
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

        private static VariableModel ParseVariableGroup(TokenGroup tokenGroup)
        {
            VariableModel variable = new VariableModel();

            if (tokenGroup.Group.Count <= 1)
            {
                // Note that tokenGroup here can never be empty since there has to be at least one token to call this function
                Output.DisplayError(new SyntaxError("Incomplete variable declaration", tokenGroup.Group[0].LineNumber));
            }

            variable.Name = tokenGroup.Group[1].Value;
            variable.LineNumber = tokenGroup.Group[0].LineNumber;
            variable.Value = tokenGroup.Group[3].Value;

            return variable;
        }

        private static TaskModel ParseTaskGroup(TokenGroup tokenGroup)
        {
            TaskModel task = new TaskModel();

            if (tokenGroup.Group.Count <= 1)
            {
                // Note that tokenGroup here can never be empty since there has to be at least one token to call this function
                Output.DisplayError(new SyntaxError("Incomplete task declaration", tokenGroup.Group[0].LineNumber));
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
                        Output.DisplayError(new SyntaxError("Invalid task declaration, dependencies can not be empty!\nUse 'null' if you don't want any dependencies", task.LineNumber));
                    }
                }
            }

            int commandsStart = indexesNewLine[0] + 1;
            int commandsEnd = indexesNewLine.Count > 1 ? indexesNewLine[1] : tokenGroup.Group.Count;
            if (commandsStart >= commandsEnd)
            {
                Output.DisplayError(new SyntaxError("Invalid task declaration, commands can not be empty", task.LineNumber));
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
                    Output.DisplayError(new SyntaxError("Invalid task declaration, commands can not be empty", task.LineNumber));
                }
            }

            return task;
        }

        #endregion
    }

    class TokenGroup
    {
        public List<Token> Group { get; set; } = new();
        public TokenType Type { get; set; }

        public TokenGroup(List<Token> group, TokenType type)
        {
            Group = group;
            Type = type;
        }

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
