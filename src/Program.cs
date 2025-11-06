using System;
using Zenith.Executor;
using Zenith.Models;
using Zenith.Parse;
using Zenith.Reader;
using Zenith.Tokenization;

// TODO: Fix line numbering: It currently is not counting comments and empty lines into the line numbering

namespace Zenith
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Zenith v0.1\n------------------");
            TaskfileReader reader = new TaskfileReader();
            reader.ReadFile("./Taskfile.txt");
            // reader.PrintContent();
            // Console.WriteLine("-----Lexer Output-----");
            Lexer lexer = new Lexer();
            List<Token> tokens = lexer.Tokenize(reader.FileContent);
            // lexer.PrintTokens(tokens);
            // Console.WriteLine("-----Parser Output-----");
            Parser parser = new Parser();
            TaskfileModel taskfileModel = parser.Parse(tokens);

            TaskExecutor exec = new TaskExecutor();
            exec.taskfileModel = taskfileModel;
            exec.ResolveDependencies("run");
            exec.PrintQueue();
        }
    }
}