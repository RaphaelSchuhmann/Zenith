using System;
using Zenith.Error;
using Zenith.Display;
using Zenith.Logs;

namespace Zenith.Models
{
    /// <summary>
    /// In-memory representation of a Taskfile. Contains parsed variables and tasks and helpers to query them.
    /// </summary>
    public class TaskfileModel
    {
        /// <summary>
        /// List of variables defined in the Taskfile.
        /// </summary>
        public List<VariableModel> Variables { get; set; } = new();

        /// <summary>
        /// List of tasks defined in the Taskfile.
        /// </summary>
        public List<TaskModel> Tasks { get; set; } = new();

        /// <summary>
        /// Finds the index of the variable with the given name in <see cref="Variables"/>.
        /// Logs an error if the variable does not exist or if duplicate variable names are detected.
        /// </summary>
        /// <param name="name">The variable name to find.</param>
        /// <returns>The index of the variable in the <see cref="Variables"/> list.</returns>
        public int FindVariableModelIndex(string name)
        {
            if (string.IsNullOrEmpty(name)) Logger.Instance.WriteError(new Internal("Variable name cannot be empty!"));

            (bool, int) isDuplicate = CheckDuplicateVariables(name);
            if (isDuplicate.Item1)
            {
                Logger.Instance.WriteError(new SyntaxError("Found more than one variable with the same name", isDuplicate.Item2));
            }

            for (int i = 0; i < Variables.Count; i++)
            {
                if (Variables[i].Name == name)
                {
                    return i;
                }
            }

            Logger.Instance.WriteError(new UserInputError($"No variable called '{name}' was found!"));

            // This part is unreachable
            return 0;
        }

        /// <summary>
        /// Checks whether more than one variable exists with the specified name.
        /// </summary>
        /// <param name="name">The variable name to check for duplicates.</param>
        /// <returns>A tuple where the first item is true when duplicates were found and the second item is the line number of the duplicate.</returns>
        public (bool, int) CheckDuplicateVariables(string name)
        {
            if (string.IsNullOrEmpty(name)) Logger.Instance.WriteError(new Internal("Variable name cannot be empty!"));

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

        /// <summary>
        /// Checks whether more than one task exists with the specified name.
        /// </summary>
        /// <param name="name">The task name to check for duplicates.</param>
        /// <returns>A tuple where the first item is true when duplicates were found and the second item is the line number of the duplicate.</returns>
        public (bool, int) CheckDuplicateTasks(string name)
        {
            if (string.IsNullOrEmpty(name)) Logger.Instance.WriteError(new Internal("Task name cannot be empty!"));

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

        /// <summary>
        /// Finds the index of the task with the given name in <see cref="Tasks"/>.
        /// Logs an error if the task is not found or if duplicate task names are detected.
        /// </summary>
        /// <param name="name">The task name to find.</param>
        /// <returns>The index of the task in the <see cref="Tasks"/> list.</returns>
        public int FindTaskModelIndex(string name)
        {
            (bool, int) isDuplicate = CheckDuplicateTasks(name);
            if (isDuplicate.Item1)
            {
                Logger.Instance.WriteError(new SyntaxError("Found more than one task with the same name", isDuplicate.Item2));
            }

            for (int i = 0; i < Tasks.Count; i++)
            {
                if (Tasks[i].Name == name)
                {
                    return i;
                }
            }

            Logger.Instance.WriteError(new UserInputError($"No task called '{name}' was found!"));

            // This part is unreachable
            return 0;
        }
    }

    /// <summary>
    /// Represents a variable declaration from the Taskfile.
    /// </summary>
    public class VariableModel
    {
        /// <summary>
        /// The variable name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The variable value (as a raw string).
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// The 1-based line number in the source Taskfile where the variable was declared.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Prints a debug representation of the variable to the configured output.
        /// </summary>
        public void PrintModel()
        {
            Output.DisplayDebug("-----Variable-----");
            Output.DisplayDebug($"Name: {Name}");
            Output.DisplayDebug($"Value: {Value}");
            Output.DisplayDebug($"LineNumber: {LineNumber}");
            Output.DisplayDebug("------------------------");
        }
    }

    /// <summary>
    /// Represents a task declaration from the Taskfile, including dependencies and commands.
    /// </summary>
    public class TaskModel
    {
        /// <summary>
        /// The task name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// List of dependency names for this task. Use the literal "null" to indicate no dependencies.
        /// </summary>
        public List<string> Dependencies { get; set; } = new();

        /// <summary>
        /// List of command strings that will be executed when this task runs.
        /// </summary>
        public List<string> Commands { get; set; } = new();

        /// <summary>
        /// The 1-based line number where the task declaration starts in the source Taskfile.
        /// </summary>
        public int LineNumber { get; set; } = 0; // Where the task starts

        /// <summary>
        /// Prints a debug representation of the task, its dependencies and commands to the configured output.
        /// </summary>
        public void PrintModel()
        {
            Output.DisplayDebug($"Name: {Name}");

            Output.DisplayDebug("Dependencies: ");
            foreach (string dep in Dependencies)
            {
                Output.DisplayDebug($"\t{dep}");
            }

            Output.DisplayDebug("Commands: ");
            foreach (string cmd in Commands)
            {
                Output.DisplayDebug($"\t{cmd}");
            }

            Output.DisplayDebug($"Line Number: {LineNumber}");
            Output.DisplayDebug("==================");
        }
    }
}
