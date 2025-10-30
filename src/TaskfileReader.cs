using System;
using System.IO;

namespace Zenith.Reader
{
    public class TaskfileReader
    {
        private string FileContent = "";
        public void ReadFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                var fileContent = File.ReadAllLines(path).ToList();

                fileContent.RemoveAll(line => line.StartsWith("#"));
                fileContent.RemoveAll(line => string.IsNullOrWhiteSpace(line));

                FileContent = string.Join(Environment.NewLine, fileContent).Trim();
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
