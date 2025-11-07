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

        public void ExecuteCommand(string cmd)
        {
            try
            {
                bool shouldOpenInTerminal = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && cmd.StartsWith(".\\")) || cmd.StartsWith("./");

                if (shouldOpenInTerminal)
                {
                    LaunchInNewTerminal(cmd);
                }
                else
                {
                    RunHeadless(cmd);
                }
            }
            catch (Exception ex)
            {
                ErrorReporter.DisplayError(new Internal($"Critical system error while executing command '{cmd}': {ex.Message}"));
            }
        }

        private void LaunchInNewTerminal(string cmd)
        {
            ProcessStartInfo startInfo;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Normalize path for Windows
                if (cmd.StartsWith("./")) cmd = ".\\" + cmd.Substring(2);
                cmd = cmd.Replace("/", "\\");

                startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k \"{cmd}\"", // Keep terminal open until user closes
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "gnome-terminal", // You can add fallback to xterm/konsole
                    Arguments = $"-- bash -c '{cmd}; exec bash'",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e 'tell application \"Terminal\" to do script \"{cmd}\"'",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
            }
            else
            {
                ErrorReporter.DisplayError(new Internal("Unsupported platform for executing commands"));
                return;
            }

            using (var process = Process.Start(startInfo))
            {
                if (process != null)
                {
                    process.WaitForExit(); // Wait until user closes the terminal
                    int exitCode = process.ExitCode;

                    if (exitCode != 0 && !(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && exitCode == unchecked((int)0xC000013A)))
                    {
                        ErrorReporter.DisplayError(new CommandError($"Command '{cmd}' failed with exit code {exitCode}."));
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Command '{cmd}' finished (terminal closed by user).");
                        Console.ResetColor();
                    }
                }
                else
                {
                    ErrorReporter.DisplayError(new Internal($"Could not start new terminal for command: {cmd}"));
                }
            }
        }

        private void RunHeadless(string cmd)
        {
            string shell;
            string argumentPrefix;

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

            string finalArgs = $"{argumentPrefix} \"{cmd}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = shell,
                Arguments = finalArgs,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    ErrorReporter.DisplayError(new Internal($"Could not start shell process: {shell}"));
                    return;
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                int exitCode = process.ExitCode;

                if (!string.IsNullOrWhiteSpace(output))
                    Console.WriteLine(output);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(error);
                    Console.ResetColor();
                }

                if (exitCode != 0)
                {
                    ErrorReporter.DisplayError(new CommandError($"Command '{cmd}' failed with exit code {exitCode}."));
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Command '{cmd}' finished successfully");
                    Console.ResetColor();
                }
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
