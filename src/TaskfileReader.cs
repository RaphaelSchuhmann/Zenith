using System;
using System.IO;
using Zenith.Display;
using Zenith.Error;
using Zenith.Logs;

namespace Zenith.Reader
{
    /// <summary>
    /// Reads and preprocesses the Taskfile from disk. Removes inline comments and exposes the cleaned file content
    /// via the <see cref="FileContent"/> field.
    /// </summary>
    public class TaskfileReader
    {
        /// <summary>
        /// The cleaned content of the Taskfile after reading and removing inline comments.
        /// </summary>
        public string FileContent = "";

        /// <summary>
        /// Reads the file at the specified path, strips inline comments and stores the result in <see cref="FileContent"/>.
        /// Logs an IO error if the file cannot be read.
        /// </summary>
        /// <param name="path">The path to the Taskfile to read.</param>
        public void ReadFile(string path)
        {
            Logger.Instance.Write("Reading Taskfile.txt", LoggerLevel.INFO);
            if (string.IsNullOrEmpty(path))
            {
                Logger.Instance.WriteError(new Internal("No file path was found"));
            }

            try
            {
                var fileContent = File.ReadAllLines(path).ToList();

                // Remove all inline comments
                List<string>? cleanedLines = new();

                foreach (string line in fileContent)
                {
                    string withoutComment = StripInlineComment(line);
                    cleanedLines.Add(withoutComment.TrimEnd());
                }

                FileContent = string.Join(Environment.NewLine, cleanedLines).Trim();
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteError(new IoError("Error while reading Taskfile", ex));
            }
        }

        /// <summary>
        /// Removes an inline comment beginning with '#' from a single line, taking into account quoted strings.
        /// Content inside single or double quotes is preserved.
        /// </summary>
        /// <param name="line">A single line from the Taskfile.</param>
        /// <returns>The line with any trailing unquoted inline comment removed.</returns>
        private static string StripInlineComment(string line)
        {
            bool inDoubleQuotes = false;
            bool inSingleQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char current = line[i];

                if (current == '"' && !inSingleQuotes)
                {
                    inDoubleQuotes = !inDoubleQuotes;
                }
                else if (current == '\'' && !inDoubleQuotes)
                {
                    inSingleQuotes = !inSingleQuotes;
                }
                else if (current == '#' && !inSingleQuotes && !inDoubleQuotes)
                {
                    return line.Substring(0, i);
                }
            }

            return line;
        }

        /// <summary>
        /// Prints the cleaned file content to the debug output, line by line. Intended for debugging only.
        /// </summary>
        public void PrintContent()
        {
            string[] lines = FileContent.Split(new[] { '\n' }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                Output.DisplayDebug(line);
            }
        }
    }
}
