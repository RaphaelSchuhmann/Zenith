using System;
using Zenith.Display;
using Zenith.Error;

namespace Zenith.Logs
{
    public sealed class Logger
    {
        private static readonly Logger _instance = new Logger();
        private readonly string LogDirectory = string.Empty;
        private readonly string LogFilePath = string.Empty;

        private Logger()
        {
            try
            {
                LogDirectory = Path.Combine(Directory.GetCurrentDirectory(), ".zenith");
                LogFilePath = Path.Combine(LogDirectory, "Zenith.Log");

                if (File.Exists(LogDirectory))
                {
                    Output.DisplayError(new Internal(".zenith exists as a file, not a directory. Please delete or rename it."));
                }

                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }
            catch (Exception ex)
            {
                Output.DisplayError(new Internal($"Logger init error: {ex.Message}"));
            }
        }

        public static Logger Instance => _instance;

        public void Write(string msg, LoggerLevel level)
        {
            string logLevel = string.Equals(level.ToString(), "IGNORE") ? "INFO" : level.ToString();
            string line = $"[{DateTime.Now:HH:mm:ss}] [{logLevel}] {msg}";

            File.AppendAllText(LogFilePath, line + Environment.NewLine);

            if (level == LoggerLevel.INFO)
            {
                Output.DisplayInfo(msg);
            }
            else if (level == LoggerLevel.WARNING)
            {
                Output.DisplayWarning(msg);
            }
            else if (level == LoggerLevel.SUCCESS)
            {
                Output.DisplaySuccess(msg);
            }
        }

        public void WriteError(ZenithException ex)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] [ERROR] {ex.Message}";
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
            Output.DisplayError(ex);
        }
    }

    public enum LoggerLevel
    {
        INFO, WARNING, ERROR, SUCCESS, IGNORE
    }
}
