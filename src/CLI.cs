using System;
using System.Reflection;
using Zenith.Display;
using Zenith.Error;
using Zenith.Executor;
using Zenith.Models;
using Zenith.Parse;
using Zenith.Reader;
using Zenith.Tokenization;

namespace Zenith.CLI
{
    public class ZenithProgram
    {
        private string CurrentDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Taskfile.txt");

        private TaskfileModel LoadTaskFile()
        {
            TaskfileReader reader = new TaskfileReader();
            reader.ReadFile(CurrentDirectory);

            Lexer lexer = new Lexer();
            List<Token> tokens = lexer.Tokenize(reader.FileContent);

            Parser parser = new Parser();
            return parser.Parse(tokens);
        }

        public void RunTask(string taskName)
        {
            TaskfileModel taskfileModel = LoadTaskFile();

            TaskExecutor exec = new TaskExecutor();
            exec.Taskfile = taskfileModel;
            exec.ResolveDependencies(taskName);
            exec.ResolveVariables();
            exec.ExecuteTasks();
        }

        public void ListTasks()
        {
            TaskfileModel taskfileModel = LoadTaskFile();

            List<TaskModel> tasks = taskfileModel.Tasks;

            foreach (TaskModel task in tasks)
            {
                Output.DisplayInfo(task.Name);
            }
        }

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
                Output.DisplayError(new Internal("Could not determine assembly information!"));
            }
        }

        public void PrintCurrentDir()
        {
            Output.DisplayDebug($"Current directory: {CurrentDirectory}");
        }
    }
}
