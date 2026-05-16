using System.IO;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace NhemDangFugBixs.NhemLogging;

public static class NhemZLoggerFactory {
    public static ILoggerFactory Create(NhemLoggerOptions? options = null) {
        options ??= new NhemLoggerOptions();

        return LoggerFactory.Create(logging => {
            logging.SetMinimumLevel(options.MinimumLevel);

            if (options.EnableFileProvider) {
                var filePath = ResolveLogFilePath(options.LogFilePath);
                var directory = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

                logging.AddZLoggerFile(filePath, zloggerOptions => {
                    if (options.UseJsonFileFormatter) zloggerOptions.UseJsonFormatter();
                });
            }

            // Tạm thời comment dòng này cho đến khi Unity provider reference đúng:
            // logging.AddZLoggerUnityDebug();
        });
    }

    private static string ResolveLogFilePath(string path) {
        if (Path.IsPathRooted(path)) return path;

        return Path.Combine(UnityEngine.Application.persistentDataPath, path);
    }
}