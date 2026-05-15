using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NhemDangFugBixs.NhemLogging {
    public class NhemUnityLogger : INhemLogger {
        private readonly ConcurrentDictionary<string, string> _colorCache = new();

        private string GetCachedColor(string className) {
            if (_colorCache.TryGetValue(className, out var cachedColor)) {
                return cachedColor;
            }
            
            var hue = (uint)className.GetHashCode() / (float)uint.MaxValue;
            var color = Color.HSVToRGB(hue, 0.6f, 1f);
            var htmlColor = ColorUtility.ToHtmlStringRGB(color);
            
            _colorCache[className] = htmlColor;
            return htmlColor;
        }

        private string FormatPrefix(string file, int line) {
            var className = Path.GetFileNameWithoutExtension(file);
            var color = GetCachedColor(className);
            return $"<color=#{color}><b><a href=\"{file}\" line=\"{line}\">[{className}:{line}]</a></b></color>: ";
        }

        public void Log(object message, Object context = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) {
            Debug.Log(FormatPrefix(file, line) + message, context);
        }

        public void LogWarning(object message, Object context = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) {
            Debug.LogWarning(FormatPrefix(file, line) + message, context);
        }

        public void LogError(object message, Object context = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) {
            Debug.LogError(FormatPrefix(file, line) + message, context);
        }
    }
}