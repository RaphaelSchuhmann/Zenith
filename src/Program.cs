using System;
using System.CommandLine;
using Zenith.CLI;
using Zenith.Display;
using Zenith.Error;
using Zenith.Logs;

namespace Zenith
{
    /// <summary>
    /// Application entry point for the Zenith command-line interface.
    /// Sets up available commands and delegates execution to <see cref="Zenith.CLI.ZenithProgram"/>.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main entry point invoked by the runtime. Initializes the logger, constructs CLI commands
        /// (run, list, version), parses arguments and invokes the selected command.
        /// Returns 0 on success or a non-zero exit code on error.
        /// </summary>
        /// <param name="args">Array of command-line arguments.</param>
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