using System;
using System.Reflection;
using Zenith.Display;
using Zenith.Error;
using Zenith.Executor;
using Zenith.Logs;
using Zenith.Models;
using Zenith.Parse;
using Zenith.Reader;
using Zenith.Tokenization;

namespace Zenith.CLI
{
    /// <summary>
    /// Entry point class for the Zenith command-line interface.
    /// Provides methods to load the Taskfile, list available tasks, run tasks and print diagnostic information.
    /// </summary>
    public class ZenithProgram
    {
        /// <summary>
        /// The absolute path to the Taskfile used by the program. Defaults to the current working directory + "Taskfile.txt".
        /// </summary>
        private string CurrentDirectory { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Taskfile.txt");

        /// <summary>
        /// Reads, tokenizes and parses the Taskfile from disk into a <see cref="TaskfileModel"/> instance.
        /// </summary>
        /// <returns>A <see cref="TaskfileModel"/> representing the parsed Taskfile.</returns>
        private TaskfileModel LoadTaskFile()
        {
            TaskfileReader reader = new TaskfileReader();
            reader.ReadFile(CurrentDirectory);

            Lexer lexer = new Lexer();
            List<Token> tokens = lexer.Tokenize(reader.FileContent);

            Parser parser = new Parser();
            return parser.Parse(tokens);
        }

        /// <summary>
        /// Executes the specified task by resolving its dependencies and variables, then running the task actions.
        /// </summary>
        /// <param name="taskName">The name of the task to execute.</param>
        public void RunTask(string taskName)
        {
            TaskfileModel taskfileModel = LoadTaskFile();

            TaskExecutor exec = new TaskExecutor();
            exec.Taskfile = taskfileModel;
            exec.ResolveDependencies(taskName);
            exec.ResolveVariables();
            exec.ExecuteTasks();
        }

        /// <summary>
        /// Enumerates and prints the names of all tasks defined in the current Taskfile.
        /// </summary>
        public void ListTasks()
        {
            TaskfileModel taskfileModel = LoadTaskFile();

            List<TaskModel> tasks = taskfileModel.Tasks;

            foreach (TaskModel task in tasks)
            {
                Output.DisplayInfo(task.Name);
            }
        }

        /// <summary>
        /// Prints the application version and the current .NET runtime version to the output.
        /// If assembly information cannot be determined an internal error is logged.
        /// </summary>
        public void PrintVersion()
        {
            Assembly? assembly = Assembly.GetEntryAssembly();

            if (assembly != null)
            {
                string? productVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                if (!string.IsNullOrEmpty(productVersion))
                {
                    Output.DisplayInfo($"Zenith: {productVersion}");
                    Output.DisplayInfo($".NET: {Environment.Version.ToString()}");
                }
                else
                {
                    Version? version = assembly.GetName().Version;
                    Output.DisplayInfo($"Zenith: {version}");
                    Output.DisplayInfo($".NET: {Environment.Version.ToString()}");
                }
            }
            else
            {
                Logger.Instance.WriteError(new Internal("Could not determine assembly information!"));
            }
        }

        /// <summary>
        /// Logs and outputs the path to the Taskfile currently used as the working directory.
        /// </summary>
        public void PrintCurrentDir()
        {
            Logger.Instance.Write($"Current directory: {CurrentDirectory}", LoggerLevel.INFO);
            Output.DisplayDebug($"Current directory: {CurrentDirectory}");
        }
    }
}
