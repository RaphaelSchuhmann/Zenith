using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Zenith.Error;
using Zenith.Models;
using Zenith.Display;
using Zenith.Logs;

namespace Zenith.Executor
{
    /// <summary>
    /// Executes tasks defined in a <see cref="TaskfileModel"/> by resolving dependencies, replacing variables
    /// and running commands either headless or in a new terminal.
    /// </summary>
    public class TaskExecutor
    {
        private readonly Queue<TaskModel> TasksQueue = new Queue<TaskModel>();
        private readonly HashSet<string> ActiveTasks = new();
        /// <summary>
        /// The taskfile model that contains all task and variable definitions to be executed.
        /// This must be set before calling <see cref="ResolveDependencies(string)"/> or <see cref="ExecuteTasks()"/>.
        /// </summary>
        public TaskfileModel Taskfile { get; set; } = new();

        /// <summary>
        /// Executes all tasks currently enqueued in the internal queue in FIFO order.
        /// Each task's commands are executed in sequence.
        /// </summary>
        public void ExecuteTasks()
        {
            if (TasksQueue.Count > 0)
            {
                foreach (TaskModel task in TasksQueue)
                {
                    Logger.Instance.Write($"Executing Task: {task.Name}", LoggerLevel.INFO);
                    List<string> commands = task.Commands;

                    foreach (string cmd in commands)
                    {
                        ExecuteCommand(cmd);
                    }
                }
            }
        }

        /// <summary>
        /// Executes a single command string. Commands that start with "./" or ".\" will open in a new terminal,
        /// otherwise they are executed headless and their output is captured.
        /// </summary>
        /// <param name="cmd">The command to execute.</param>
        public void ExecuteCommand(string cmd)
        {
            Logger.Instance.Write($"Executing command: {cmd}", LoggerLevel.IGNORE);
            try
            {
                bool shouldOpenInTerminal = cmd.StartsWith("./") || cmd.StartsWith(".\\");

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
                Logger.Instance.WriteError(new Internal($"Critical system error while executing command '{cmd}': {ex.Message}"));
            }
        }

        /// <summary>
        /// Launches the provided command in a new platform-appropriate terminal window and waits for it to close.
        /// Errors starting the terminal or non-zero exit codes are logged as command errors.
        /// </summary>
        private void LaunchInNewTerminal(string cmd)
        {
            Logger.Instance.Write($"Executing command {cmd} in a new terminal", LoggerLevel.IGNORE);
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
                    Arguments = $"-- bash -c '{cmd.Replace("'", "'\\''")}; exec bash'",
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
                    Arguments = $"-e 'tell application \"Terminal\" to do script \"{cmd.Replace("\"", "\\\\\\\"")}\"'",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
            }
            else
            {
                Logger.Instance.WriteError(new Internal("Unsupported platform for executing commands"));
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
                        Logger.Instance.WriteError(new CommandError($"Command '{cmd}' failed with exit code {exitCode}."));
                    }
                    else
                    {
                        Logger.Instance.Write($"Command '{cmd}' finished (terminal closed by user).", LoggerLevel.SUCCESS);
                    }
                }
                else
                {
                    Logger.Instance.WriteError(new Internal($"Could not start new terminal for command: {cmd}"));
                }
            }
        }

        /// <summary>
        /// Executes the provided command in a headless shell, captures stdout/stderr and logs failures as command errors.
        /// </summary>
        /// <param name="cmd">The command to run in a headless shell.</param>
        private void RunHeadless(string cmd)
        {
            Logger.Instance.Write($"Executing command {cmd} headless.", LoggerLevel.IGNORE);
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
                    Logger.Instance.WriteError(new Internal($"Could not start shell process: {shell}"));
                    return; // Unreachable but needed to silence compiler
                }

                process.WaitForExit();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                int exitCode = process.ExitCode;

                if (!string.IsNullOrWhiteSpace(output))
                    Console.WriteLine(output);

                if (exitCode != 0)
                {
                    Logger.Instance.WriteError(new CommandError($"Command '{cmd}' failed with exit code {exitCode}.\nError output: {error}"));
                }
                else
                {
                    Logger.Instance.Write($"Command '{cmd}' finished successfully.", LoggerLevel.SUCCESS);
                }
            }
        }

        /// <summary>
        /// Recursively resolves dependencies for the named task and enqueues tasks in order of execution.
        /// Detects duplicate tasks and circular dependencies and logs errors accordingly.
        /// </summary>
        /// <param name="taskName">The name of the task to resolve and enqueue.</param>
        public void ResolveDependencies(string taskName)
        {
            Logger.Instance.Write($"Resolving dependency: {taskName}", LoggerLevel.IGNORE);

            if (string.IsNullOrEmpty(taskName))
            {
                Logger.Instance.WriteError(new UserInputError("Task name cannot be empty!"));
            }

            TaskModel mainTask = Taskfile.Tasks[Taskfile.FindTaskModelIndex(taskName)];

            if (!ActiveTasks.Add(mainTask.Name))
            {
                Logger.Instance.WriteError(new SyntaxError("Infinite loop detected", mainTask.LineNumber));
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
                            Logger.Instance.WriteError(new SyntaxError("Found more than one task with the same name", isDuplicate.Item2));
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

        /// <summary>
        /// Replaces variable placeholders (${name}) in each queued task's commands using the variables defined in the taskfile.
        /// </summary>
        public void ResolveVariables()
        {
            Logger.Instance.Write("Resolving Variables", LoggerLevel.IGNORE);
            if (TasksQueue.Count > 0)
            {
                foreach (TaskModel task in TasksQueue)
                {
                    List<string> commands = task.Commands;

                    // In theory this will never be true as it should already have checked that before
                    if (commands.Count <= 0) Logger.Instance.WriteError(new SyntaxError("Invalid task declaration, commands can not be empty", task.LineNumber));

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

        /// <summary>
        /// Prints the current execution queue for debugging purposes.
        /// </summary>
        public void PrintQueue()
        {
            Output.DisplayDebug("===== Queue =====");
            foreach (TaskModel task in TasksQueue)
            {
                task.PrintModel();
            }
        }

        #region Helpers

        /// <summary>
        /// Returns whether the internal task queue already contains a task with the given name.
        /// </summary>
        /// <param name="name">The task name to search for.</param>
        /// <returns>True if the queue contains a task with the name; otherwise false.</returns>
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
