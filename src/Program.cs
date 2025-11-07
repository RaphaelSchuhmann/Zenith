using System;
using System.CommandLine;
using Zenith.CLI;
using Zenith.Display;
using Zenith.Error;

namespace Zenith
{
    internal class Program
    {
        static int Main(string[] args)
        {
            ZenithProgram zenith = new ZenithProgram();

            RootCommand rootCommand = new("An app for automating tasks");

            Command runCommand = new("run", "Executes a given task");
            Argument<string> runArg = new Argument<string>("taskName") { Arity = ArgumentArity.ExactlyOne };

            runCommand.Arguments.Add(runArg);

            runCommand.SetAction(parseResult =>
            {
                string? taskName = parseResult.GetValue(runArg);
                if (string.IsNullOrEmpty(taskName))
                {
                    Output.DisplayError(new UserInputError("Task name cannot be empty"));
                    return 1;
                }

                zenith.RunTask(taskName);
                return 0;
            });

            Command listCommand = new("list", "Displays all commands in Taskfile.txt");

            listCommand.SetAction(parseResult =>
            {
                zenith.ListTasks();
                return 0;
            });

            Command versionCommand = new("--v", "Displays the currently installed version of Zenith and .NET");

            versionCommand.SetAction(parseResult =>
            {
                zenith.PrintVersion();
                return 0;
            });

            rootCommand.Subcommands.Add(runCommand);
            rootCommand.Subcommands.Add(listCommand);
            rootCommand.Subcommands.Add(versionCommand);

            ParseResult parseResult = rootCommand.Parse(args);
            return parseResult.Invoke();
        }
    }
}