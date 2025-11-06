using System;
using Zenith.Error;

namespace Zenith.Models
{
    public class TaskfileModel
    {
        public List<VariableModel> Variables { get; set; } = new();
        public List<TaskModel> Tasks { get; set; } = new();

        public int FindVariableModelIndex(string name)
        {
            if (string.IsNullOrEmpty(name)) ErrorReporter.DisplayError(new Internal("Variable name cannot be empty!"));

            (bool, int) isDuplicate = CheckDuplicateVariables(name);
            if (isDuplicate.Item1)
            {
                ErrorReporter.DisplayError(new SyntaxError("Found more than one task with the same name", isDuplicate.Item2));
            }

            for (int i = 0; i < Variables.Count; i++)
            {
                if (Variables[i].Name == name)
                {
                    return i;
                }
            }

            ErrorReporter.DisplayError(new UserInputError($"No variable called '{name}' was found!"));

            // This part is unreachable
            return 0;
        }

        public (bool, int) CheckDuplicateVariables(string name)
        {
            if (string.IsNullOrEmpty(name)) ErrorReporter.DisplayError(new Internal("Variable name cannot be empty!"));

            int count = 0;

            foreach (VariableModel variable in Variables)
            {
                if (variable.Name == name)
                {
                    count++;
                    if (count > 1)
                    {
                        return (true, variable.LineNumber);
                    }
                }
            }

            return (false, 0);
        }

        public (bool, int) CheckDuplicateTasks(string name)
        {
            if (string.IsNullOrEmpty(name)) ErrorReporter.DisplayError(new Internal("Task name cannot be empty!"));

            int count = 0;

            foreach (TaskModel task in Tasks)
            {
                if (task.Name == name)
                {
                    count++;
                    if (count > 1)
                    {
                        return (true, task.LineNumber);
                    }
                }
            }

            return (false, 0);
        }

        public int FindTaskModelIndex(string name)
        {
            (bool, int) isDuplicate = CheckDuplicateTasks(name);
            if (isDuplicate.Item1)
            {
                ErrorReporter.DisplayError(new SyntaxError("Found more than one task with the same name", isDuplicate.Item2));
            }

            for (int i = 0; i < Tasks.Count; i++)
            {
                if (Tasks[i].Name == name)
                {
                    return i;
                }
            }

            ErrorReporter.DisplayError(new UserInputError($"No task called '{name}' was found!"));

            // This part is unreachable
            return 0;
        }
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
