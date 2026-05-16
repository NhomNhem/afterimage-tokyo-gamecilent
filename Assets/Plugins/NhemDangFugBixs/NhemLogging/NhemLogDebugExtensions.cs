using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NhemDangFugBixs.NhemLogging
{
    public static class NhemLogDebugExtensions
    {
        [Conditional("GR_INPUT_DEBUG")]
        public static void InputDebug(
            this INhemLogger logger,
            object? message,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.Log(message, context, file, line);
        }

        [Conditional("GR_COMBAT_DEBUG")]
        public static void CombatDebug(
            this INhemLogger logger,
            object? message,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.Log(message, context, file, line);
        }

        [Conditional("GR_MEMORY_DEBUG")]
        public static void MemoryDebug(
            this INhemLogger logger,
            object? message,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.Log(message, context, file, line);
        }

        [Conditional("GR_ENCOUNTER_DEBUG")]
        public static void EncounterDebug(
            this INhemLogger logger,
            object? message,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.Log(message, context, file, line);
        }

        [Conditional("GR_TARGETING_DEBUG")]
        public static void TargetingDebug(
            this INhemLogger logger,
            object? message,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.Log(message, context, file, line);
        }

        [Conditional("GR_CAMERA_DEBUG")]
        public static void CameraDebug(
            this INhemLogger logger,
            object? message,
            Object? context = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            logger.Log(message, context, file, line);
        }
    }
}