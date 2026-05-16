using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NhemDangFugBixs.NhemLogging;

public sealed class NhemUnityLogger : INhemLogger {
    private readonly ConcurrentDictionary<string, string> colorCache = new();

    public void Log(
        object? message,
        Object? context = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) {
        Debug.Log(FormatPrefix(file, line) + (message?.ToString() ?? "null"), context);
    }

    public void LogWarning(
        object? message,
        Object? context = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) {
        Debug.LogWarning(FormatPrefix(file, line) + (message?.ToString() ?? "null"), context);
    }

    public void LogError(
        object? message,
        Object? context = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) {
        Debug.LogError(FormatPrefix(file, line) + (message?.ToString() ?? "null"), context);
    }

    private string FormatPrefix(string file, int line) {
        var className = Path.GetFileNameWithoutExtension(file);

        if (string.IsNullOrWhiteSpace(className)) className = "Unknown";

        var color = GetCachedColor(className);
        var safeFile = string.IsNullOrWhiteSpace(file)
            ? string.Empty
            : file.Replace("\\", "/").Replace("\"", "%22");

        if (string.IsNullOrWhiteSpace(safeFile)) return $"<color=#{color}><b>[{className}:{line}]</b></color>: ";

        return $"<color=#{color}><b><a href=\"{safeFile}\" line=\"{line}\">[{className}:{line}]</a></b></color>: ";
    }

    private string GetCachedColor(string className) {
        return colorCache.GetOrAdd(className, static name => {
            var hue = StableHash(name) / (float)uint.MaxValue;
            var color = Color.HSVToRGB(hue, 0.6f, 1f);
            return ColorUtility.ToHtmlStringRGB(color);
        });
    }

    private static uint StableHash(string value) {
        unchecked {
            var hash = 2166136261;

            for (var i = 0; i < value.Length; i++) hash = (hash ^ value[i]) * 16777619;

            return hash;
        }
    }
}