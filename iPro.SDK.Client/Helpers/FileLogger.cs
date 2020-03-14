using System;
using System.IO;

namespace iPro.SDK.Client.Helpers
{
    public class FileLogger
    {
        public const string LogsName = "logs";

        public FileLogger()
        {
            var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogsName);

            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            FilePath = Path.Combine(logsDir, DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
        }

        public string FilePath { get; set; }

        public void Write(string content)
        {
            using (var stream = new FileStream(FilePath, FileMode.Append))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(content);
                }
            }
        }

        public void Error(string title, string error)
        {
            Write($"[error] {title} ${error}");
        }

        public void Info(string title, string message)
        {
            Write($"[info] {title} ${message}");
        }
    }
}
