using System;
using System.IO;

namespace Zenith.Reader
{
    public class TaskfileReader
    {
        public string FileContent = "";
        public void ReadFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("No Taskfile was provided.");
                return;
            }

            try
            {
                var fileContent = File.ReadAllLines(path).ToList();

                fileContent.RemoveAll(line => line.StartsWith("#"));
                fileContent.RemoveAll(line => string.IsNullOrWhiteSpace(line));
                
                // Remove all inline comments
                List<string>? cleanedLines = new();
                
                foreach (var line in fileContent)
                {
                    int index = line.IndexOf("#");
                    if (index > -1)
                    {
                        cleanedLines.Add(line.Substring(0, index));
                    }
                    else
                    {
                        cleanedLines.Add(line);
                    }
                }

                FileContent = string.Join(Environment.NewLine, cleanedLines).Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
                FileContent = "";
            }
        }

        // DEBUG:
        public void PrintContent()
        {
            Console.WriteLine(FileContent);
        }
    }
}
