using Microsoft.Extensions.Logging;

namespace NhemDangFugBixs.NhemLogging
{
    public sealed class NhemLoggerOptions
    {
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

        public bool EnableUnityDebugProvider { get; set; } = true;

        public bool EnableFileProvider { get; set; }

        public bool UseJsonFileFormatter { get; set; } = true;

        public string LogFilePath { get; set; } = "Logs/glass-refrain-session.log";
    }
}