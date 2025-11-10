using System;
using System.CommandLine;
using Zenith.CLI;
using Zenith.Display;
using Zenith.Error;
using Zenith.Logs;

namespace Zenith
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Logger.Instance.Write("Starting Zenith run...", LoggerLevel.IGNORE);

            ZenithProgram zenith = new ZenithProgram();

            RootCommand rootCommand = new("An app for automating tasks");

            Command runCommand = new("run", "Executes a given task");
            Argument<string> runArg = new Argument<string>("taskName") { Arity = ArgumentArity.ExactlyOne };

            runCommand.Arguments.Add(runArg);

            runCommand.SetAction(parseResult =>
            {
                Logger.Instance.Write("Used 'run' command.", LoggerLevel.IGNORE);
                string? taskName = parseResult.GetValue(runArg);
                if (string.IsNullOrEmpty(taskName))
                {
                    Logger.Instance.WriteError(new UserInputError("Task name cannot be empty"));
                    return 1;
                }

                zenith.RunTask(taskName);
                return 0;
            });

            Command listCommand = new("list", "Displays all commands in Taskfile.txt");

            listCommand.SetAction(parseResult =>
            {
                Logger.Instance.Write("Listing tasks.", LoggerLevel.IGNORE);
                zenith.ListTasks();
                return 0;
            });

            Command versionCommand = new("--v", "Displays the currently installed version of Zenith and .NET");

            versionCommand.SetAction(parseResult =>
            {
                Logger.Instance.Write("Displaying version.", LoggerLevel.IGNORE);
                zenith.PrintVersion();
                return 0;
            });

            rootCommand.Subcommands.Add(runCommand);
            rootCommand.Subcommands.Add(listCommand);
            rootCommand.Subcommands.Add(versionCommand);

            ParseResult parseResult = rootCommand.Parse(args);
            int exitCode = parseResult.Invoke();
            if (exitCode == 0) Logger.Instance.Write("Zenith exited successfully", LoggerLevel.IGNORE);
            return exitCode;
        }
    }
}