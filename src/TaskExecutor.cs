using System;
using System.Text.RegularExpressions;
using Zenith.Error;
using Zenith.Models;

namespace Zenith.Executor
{
    public class TaskExecutor
    {
        private readonly Queue<TaskModel> TasksQueue = new Queue<TaskModel>();
        private readonly HashSet<string> ActiveTasks = new();
        public TaskfileModel Taskfile { get; set; } = new();

        public void ResolveDependencies(string taskName)
        {
            if (string.IsNullOrEmpty(taskName))
            {
                ErrorReporter.DisplayError(new UserInputError("Task name cannot be empty!"));
            }

            TaskModel mainTask = Taskfile.Tasks[Taskfile.FindTaskModelIndex(taskName)];

            if (!ActiveTasks.Add(mainTask.Name))
            {
                ErrorReporter.DisplayError(new SyntaxError("Infinite loop detected", mainTask.LineNumber));
            }

            try
            {
                List<string> dependencies = mainTask.Dependencies;

                if (dependencies.Count > 0 && !string.Equals(dependencies[0], "null"))
                {
                    foreach (string dep in dependencies)
                    {
                        (bool, int) isDuplicate = Taskfile.CheckDuplicateTasks(dep);
                        if (isDuplicate.Item1)
                        {
                            ErrorReporter.DisplayError(new SyntaxError("Found more than one task with the same name", isDuplicate.Item2));
                        }

                        if (!TaskQueueContains(dep))
                        {
                            ResolveDependencies(dep);
                        }
                    }
                }

                if (!TaskQueueContains(mainTask.Name))
                {
                    TasksQueue.Enqueue(mainTask);
                }
            }
            finally
            {
                ActiveTasks.Remove(mainTask.Name);
            }
        }

        public void ResolveVariables()
        {
            if (TasksQueue.Count > 0)
            {
                foreach (TaskModel task in TasksQueue)
                {
                    List<string> commands = task.Commands;

                    // In theory this will never be true as it should already have checked that before
                    if (commands.Count <= 0) ErrorReporter.DisplayError(new SyntaxError("Invalid task declaration, commands can not be empty", task.LineNumber));

                    string pattern = @"\$\{([A-Za-z_][A-Za-z0-9_]+)\}";

                    List<string> updatedCommands = new();

                    foreach (string cmd in commands)
                    {
                        string resolveCmd = Regex.Replace(cmd, pattern, match =>
                        {
                            string varName = match.Groups[1].Value;
                            VariableModel variable = Taskfile.Variables[Taskfile.FindVariableModelIndex(varName)];
                            return variable.Value;
                        });
                        updatedCommands.Add(resolveCmd);
                    }

                    task.Commands = updatedCommands;
                }
            }
        }

        public void PrintQueue()
        {
            Console.WriteLine("=====Queue=====");
            foreach (TaskModel task in TasksQueue)
            {
                task.PrintModel();
            }
        }

        #region Helpers

        private bool TaskQueueContains(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            foreach (TaskModel task in TasksQueue)
            {
                if (task.Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
