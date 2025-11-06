using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

        public void ExecuteTasks()
        {
            if (TasksQueue.Count > 0)
            {
                foreach (TaskModel task in TasksQueue)
                {
                    Console.WriteLine($"Task: {task.Name}");
                    List<string> commands = task.Commands;

                    foreach (string cmd in commands)
                    {
                        ExecuteCommand(cmd);
                    }
                }
            }
        }

        // ! This function is unable to execute a command like ./bin/main.exe
        public void ExecuteCommand(string cmd)
        {
            string shell;
            string argumentPrefix;
            string commandToExecute = cmd;

            string workingDirectory = Directory.GetCurrentDirectory();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shell = "cmd.exe";
                argumentPrefix = "/C";
            }
            else
            {
                shell = "/bin/bash";
                argumentPrefix = "-c";
            }

            string finalArguments = $"{argumentPrefix}\"{commandToExecute}\"";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = shell,
                    Arguments = finalArguments,

                    WorkingDirectory = workingDirectory,

                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false, // Must be false when RedirectStandard* is true
                    CreateNoWindow = true,
                };

                using (Process? process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        ErrorReporter.DisplayError(new Internal($"Could not start shell process: {shell}"));
                    }
                    else
                    {
                        process.WaitForExit();

                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        int exitCode = process.ExitCode;

                        if (exitCode != 0)
                        {
                            ErrorReporter.DisplayError(new CommandError($"Command '{cmd}' failed with exit code {exitCode}. Error output: {error}"));
                        }

                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Command '{cmd}' finished successfully");
                            Console.ResetColor();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorReporter.DisplayError(new Internal($"Critical system error while attempting to run shell '{shell}': {ex.Message}"));
            }
        }

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
