using System;
using Zenith.Reader;

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
        }
    }
}