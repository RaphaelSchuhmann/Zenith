using System;
using System.Net.WebSockets;
using System.Runtime.Versioning;
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
                Console.WriteLine("Error: Parser encountered an empty list of tokens!");
                return taskFile;
            }

            // Group tokens
            // The actual token type is irrelevant for this search
            List<int> keywordIndexes = GetTokenIndexes(tokens, new Token(TokenType.NEWLINE, string.Empty, 0, TokenType.KEYWORD), true);

            List<TokenGroup> tokenGroups = new();

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
                    // Throw a syntax error, invalid Taskfile
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

            if (!tokenGroup.Group.Any())
            {
                // Throw warning / error of invalid variable in line x
                return variable;
            }

            variable.Name = tokenGroup.Group[1].Value;
            variable.LineNumber = tokenGroup.Group[0].LineNumber;
            variable.Value = tokenGroup.Group[3].Value;

            return variable;
        }

        private static TaskModel ParseTaskGroup(TokenGroup tokenGroup)
        {
            TaskModel task = new TaskModel();

            if (!tokenGroup.Group.Any())
            {
                // Throw warning / error of invalid variable in line x
                return task;
            }

            task.Name = tokenGroup.Group[1].Value;

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
                        // Throw error cause empty dependency
                    }
                }
            }

            if (1 > indexesNewLine.Count - 1)
            {
                List<Token> commands = tokenGroup.Group[indexesNewLine[0]..];
                foreach (Token cmd in commands)
                {
                    if (cmd.Value != "")
                    {
                        task.Commands.Add(cmd.Value);
                    }
                    else
                    {
                        // Throw error cause empty command
                    }
                }
            }
            else
            {
                List<Token> commands = tokenGroup.Group[indexesNewLine[0]..indexesNewLine[1]];
                foreach (Token cmd in commands)
                {
                    if (cmd.Value != "")
                    {
                        task.Commands.Add(cmd.Value);
                    }
                    else
                    {
                        // Throw error cause empty command
                    }
                }
            }

            task.LineNumber = tokenGroup.Group[0].LineNumber;

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
            Console.WriteLine("-----Printing Group-----");
            Console.WriteLine($"Type: {Type}");
            Console.WriteLine("Tokens: ");
            Console.WriteLine("---");
            foreach (Token token in Group)
            {
                token.PrintToken();
            }
        }
    }
}
