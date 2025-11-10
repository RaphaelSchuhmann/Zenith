using System;
using System.IO;
using Zenith.Display;
using Zenith.Error;
using Zenith.Logs;

namespace Zenith.Reader
{
    public class TaskfileReader
    {
        public string FileContent = "";
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

        // DEBUG:
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
