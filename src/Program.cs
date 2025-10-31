﻿using System;
using Zenith.Reader;
using Zenith.Tokenization;

namespace Zenith
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Zenith v0.1\n------------------");
            TaskfileReader reader = new TaskfileReader();
            reader.ReadFile("./Taskfile.txt");
            reader.PrintContent();
            Console.WriteLine("-----------------------------");
            Lexer lexer = new Lexer();
            List<Token> tokens = lexer.Tokenize(reader.FileContent);
            lexer.PrintTokens(tokens);
        }
    }
}