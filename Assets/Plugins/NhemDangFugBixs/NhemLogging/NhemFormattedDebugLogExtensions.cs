using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NhemDangFugBixs.NhemLogging
{
    public static class NhemFormattedDebugLogExtensions
    {
        [Conditional("GR_COMBAT_DEBUG")]
        public static void CombatDebugFormat<T1, T2, T3>(
            this INhemLogger logger,
            string format,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.Log(ZString.Format(format, arg1, arg2, arg3), context, file, line);
        }

        [Conditional("GR_MEMORY_DEBUG")]
        public static void MemoryDebugFormat<T1, T2>(
            this INhemLogger logger,
            string format,
            T1 arg1,
            T2 arg2,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.Log(ZString.Format(format, arg1, arg2), context, file, line);
        }
    }
}