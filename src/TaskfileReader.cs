using System;
using System.IO;
using Zenith.Error;

namespace Zenith.Reader
{
    public class TaskfileReader
    {
        public string FileContent = "";
        public void ReadFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                ErrorReporter.DisplayError(new Internal("No file path was found"));
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
                ErrorReporter.DisplayError(new IoError("Error while reading Taskfile", ex));
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
            Console.WriteLine(FileContent);
        }
    }
}
