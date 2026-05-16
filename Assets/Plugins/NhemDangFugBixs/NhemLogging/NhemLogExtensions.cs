using Cysharp.Text;
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
            string file = "",
            int line = 0)
        {
            logger.Log(ZString.Format(format, arg1), context, file, line);
        }

        public static void LogFormat<T1, T2>(
            this INhemLogger logger,
            string format,
            T1 arg1,
            T2 arg2,
            Object? context = null,
            string file = "",
            int line = 0)
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
            string file = "",
            int line = 0)
        {
            logger.Log(ZString.Format(format, arg1, arg2, arg3), context, file, line);
        }
    }
}