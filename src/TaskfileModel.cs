using System;

namespace Zenith.Models
{
    public class TaskfileModel
    {
        public List<VariableModel> Variables { get; set; } = new();
        public List<TaskModel> Tasks { get; set; } = new();
    }

    public class VariableModel
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int LineNumber { get; set; }

        public void PrintModel()
        {
            Console.WriteLine("-----Variable-----");
            Console.WriteLine($"Name: {Name}");
            Console.WriteLine($"Value: {Value}");
            Console.WriteLine($"LineNumber: {LineNumber}");
            Console.WriteLine("------------------------");
        }
    }

    public class TaskModel
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
        public List<string> Commands { get; set; } = new();
        public int LineNumber { get; set; } = 0; // Where the task starts

        public void PrintModel()
        {
            Console.WriteLine("-----Task-----");
            Console.WriteLine($"Name: {Name}");

            Console.WriteLine("Dependencies: ");
            foreach (string dep in Dependencies)
            {
                Console.WriteLine($"\t{dep}");
            }

            Console.WriteLine("Commands:");
            foreach (string cmd in Commands)
            {
                Console.WriteLine($"\t{cmd}");
            }

            Console.WriteLine($"LineNumber: {LineNumber}");
            Console.WriteLine("------------------------");
        }
    }
}
