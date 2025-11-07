using System;
using Zenith.Executor;
using Zenith.Models;
using Zenith.Parse;
using Zenith.Reader;
using Zenith.Tokenization;

namespace Zenith
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TaskfileReader reader = new TaskfileReader();
            reader.ReadFile("./Taskfile.txt");
            // reader.PrintContent();
            
            Lexer lexer = new Lexer();
            List<Token> tokens = lexer.Tokenize(reader.FileContent);
            // lexer.PrintTokens(tokens);

            Parser parser = new Parser();
            TaskfileModel taskfileModel = parser.Parse(tokens);

            TaskExecutor exec = new TaskExecutor();
            exec.Taskfile = taskfileModel;
            exec.ResolveDependencies("run");
            exec.ResolveVariables();
            // exec.PrintQueue();
            // exec.ExecuteTasks();
        }
    }
}