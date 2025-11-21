using System;
using Zenith.Display;
using Zenith.Error;

namespace Zenith.Logs
{
    /// <summary>
    /// Provides simple file-based logging for the application and exposes a singleton instance via <see cref="Instance"/>.
    /// The logger writes timestamped entries to a log file located in the .zenith directory below the current working directory
    /// and forwards informational messages to the <see cref="Output"/> helper for user-visible output.
    /// </summary>
    public sealed class Logger
    {
        private static readonly Logger _instance = new Logger();
        private readonly string? LogDirectory;
        private readonly string? LogFilePath;

        private Logger()
        {
            try
            {
                LogDirectory = Path.Combine(Directory.GetCurrentDirectory(), ".zenith");
                LogFilePath = Path.Combine(LogDirectory, "Zenith.Log");

                if (LogDirectory == null)
                {
                    throw new Internal("Log directory cannot be null!");
                }

                if (LogFilePath == null)
                {
                    throw new Internal("Log file path cannot be null!");
                }

                if (File.Exists(LogDirectory) && !Directory.Exists(LogDirectory))
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

        /// <summary>
        /// Gets the global singleton instance of <see cref="Logger"/>.
        /// </summary>
        public static Logger Instance => _instance;

        /// <summary>
        /// Writes a message to the log file with the specified severity. Certain levels are also displayed to the console
        /// using the <see cref="Output"/> helper (INFO, WARNING, SUCCESS).
        /// </summary>
        /// <param name="msg">The message to write to the log.</param>
        /// <param name="level">The severity level of the log entry.</param>
        public void Write(string msg, LoggerLevel level)
        {
            if (LogFilePath == null) return;
            
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

        /// <summary>
        /// Writes an error entry for the provided <see cref="ZenithException"/> to the log and displays the error to the user.
        /// </summary>
        /// <param name="ex">The exception to log and display.</param>
        public void WriteError(ZenithException ex)
        {
            if (LogFilePath == null) return;

            string line = $"[{DateTime.Now:HH:mm:ss}] [ERROR] {ex.Message}";
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
            Output.DisplayError(ex);
        }
    }

    /// <summary>
    /// Severity levels used by the logger to categorize messages and determine presentation behaviour.
    /// </summary>
    public enum LoggerLevel
    {
        INFO, WARNING, ERROR, SUCCESS, IGNORE
    }
}
