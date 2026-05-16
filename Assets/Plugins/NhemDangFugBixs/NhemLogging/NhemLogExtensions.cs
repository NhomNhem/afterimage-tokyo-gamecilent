using System.Runtime.CompilerServices;
using Cysharp.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NhemDangFugBixs.NhemLogging
{
    public static class NhemLogExtensions
    {
        public static void LogFormat<T1>(
            this INhemLogger logger,
            string format,
            T1 arg1,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.Log(ZString.Format(format, arg1), context, file, line);
        }

        public static void LogFormat<T1, T2>(
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

        public static void LogFormat<T1, T2, T3>(
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

        public static void LogWarningFormat<T1>(
            this INhemLogger logger,
            string format,
            T1 arg1,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.LogWarning(ZString.Format(format, arg1), context, file, line);
        }

        public static void LogErrorFormat<T1>(
            this INhemLogger logger,
            string format,
            T1 arg1,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.LogError(ZString.Format(format, arg1), context, file, line);
        }
    }
}