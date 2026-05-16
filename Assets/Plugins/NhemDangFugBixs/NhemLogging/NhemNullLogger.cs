using UnityEngine;
using Object = UnityEngine.Object;

namespace NhemDangFugBixs.NhemLogging;

public sealed class NhemNullLogger : INhemLogger {
    public void Log(object? message, Object? context = null, string file = "", int line = 0) { }

    public void LogWarning(object? message, Object? context = null, string file = "", int line = 0) { }

    public void LogError(object? message, Object? context = null, string file = "", int line = 0) { }
}