using System;
using System.IO;

namespace Zenith.Reader
{
    public class TaskfileReader
    {
        private string FileContent = "";
        public void ReadFile(string path)
        {
            if (path.Length <= 0)
            {
                return;
            }

            var fileContent = File.ReadAllLines(path).ToList();

            fileContent.RemoveAll(line => line.StartsWith("#"));
            fileContent.RemoveAll(line => string.IsNullOrWhiteSpace(line));
            
            FileContent = string.Join(Environment.NewLine, fileContent).Trim();
        }

        // DEBUG:
        public void PrintContent()
        {
            Console.WriteLine(FileContent);
        }
    }
}
