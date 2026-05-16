using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using Microsoft.Extensions.Logging;
using UnityEngine;
using Object = UnityEngine.Object;
using ZLogger;

namespace NhemDangFugBixs.NhemLogging
{
    public sealed class NhemZLogger : INhemLogger
    {
        private readonly ILogger<NhemZLogger> logger;
        private readonly ConcurrentDictionary<string, string> colorCache = new();

        public NhemZLogger(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<NhemZLogger>();
        }

        public void Log(
            object? message,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.ZLogInformation($"{FormatMessage(message, file, line)}");
        }

        public void LogWarning(
            object? message,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.ZLogWarning($"{FormatMessage(message, file, line)}");
        }

        public void LogError(
            object? message,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.ZLogError($"{FormatMessage(message, file, line)}");
        }

        private string FormatMessage(object? message, string file, int line)
        {
            var className = Path.GetFileNameWithoutExtension(file);

            if (string.IsNullOrWhiteSpace(className))
            {
                className = "Unknown";
            }

            var color = GetCachedColor(className);
            var safeFile = string.IsNullOrWhiteSpace(file)
                ? string.Empty
                : file.Replace("\\", "/").Replace("\"", "%22");

            using var sb = ZString.CreateStringBuilder();

            if (string.IsNullOrWhiteSpace(safeFile))
            {
                sb.Append("<color=#");
                sb.Append(color);
                sb.Append("><b>[");
                sb.Append(className);
                sb.Append(":");
                sb.Append(line);
                sb.Append("]</b></color>: ");
            }
            else
            {
                sb.Append("<color=#");
                sb.Append(color);
                sb.Append("><b><a href=\"");
                sb.Append(safeFile);
                sb.Append("\" line=\"");
                sb.Append(line);
                sb.Append("\">[");
                sb.Append(className);
                sb.Append(":");
                sb.Append(line);
                sb.Append("]</a></b></color>: ");
            }

            sb.Append(message?.ToString() ?? "null");
            return sb.ToString();
        }

        private string GetCachedColor(string className)
        {
            return colorCache.GetOrAdd(className, static name =>
            {
                var hue = StableHash(name) / (float)uint.MaxValue;
                var color = Color.HSVToRGB(hue, 0.6f, 1f);
                return ColorUtility.ToHtmlStringRGB(color);
            });
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;

                for (var i = 0; i < value.Length; i++)
                {
                    hash = (hash ^ value[i]) * 16777619;
                }

                return hash;
            }
        }
    }
}