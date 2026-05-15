using System.Runtime.CompilerServices;
using UnityEngine;

namespace NhemDangFugBixs.NhemLogging {
    public interface INhemLogger {
        void Log(
            object message, 
            Object context = null, 
            [CallerFilePath] string file = "", 
            [CallerLineNumber] int line = 0);
            
        void LogWarning(
            object message, 
            Object context = null, 
            [CallerFilePath] string file = "", 
            [CallerLineNumber] int line = 0);
            
        void LogError(
            object message, 
            Object context = null, 
            [CallerFilePath] string file = "", 
            [CallerLineNumber] int line = 0);
    }
}