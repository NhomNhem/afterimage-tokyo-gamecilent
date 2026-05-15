using UnityEngine;

namespace NhemDangFugBixs.NhemLogging {
    public class NhemNullLogger : INhemLogger {
        public void Log(object message, Object context = null, string file = "", int line = 0) { }
        public void LogWarning(object message, Object context = null, string file = "", int line = 0) { }
        public void LogError(object message, Object context = null, string file = "", int line = 0) { }
    }
}