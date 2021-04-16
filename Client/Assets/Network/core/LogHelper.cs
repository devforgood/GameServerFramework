using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
namespace Serilog
{
    public static class Log
    {
        [System.Diagnostics.Conditional("USE_LOG")]
        public static void Information(string msg)
        {
            Debug.Log(msg);
        }

        [System.Diagnostics.Conditional("USE_LOG")]
        public static void Warning(string msg)
        {
            Debug.LogWarning(msg);
        }

        [System.Diagnostics.Conditional("USE_LOG")]
        public static void Error(string msg)
        {
            Debug.LogError(msg);
        }
    }
}
#else
using Serilog;

#endif

namespace core
{
    /// <summary>
    /// Log level
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Developer logging level, most verbose
        /// </summary>
        Developer,
        /// <summary>
        /// Normal logging level, medium verbose
        /// </summary>
        Normal,
        /// <summary>
        /// Error logging level, very quiet
        /// </summary>
        Error,
        /// <summary>
        /// Nothing logging level, no logging will be done
        /// </summary>
        Nothing
    }

    /// <summary>
    /// Helper class for logging
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// Gets the current log level.
        /// </summary>
        /// <value>The current log level.</value>
        public static LogLevel CurrentLogLevel
        {
            get
            {
                return LogLevel.Normal;
            }
        }


        [System.Diagnostics.Conditional("USE_LOG")]
        public static void LogInfo(string msg)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
            Debug.Log(msg);
#else
            Log.Information(msg);
#endif
        }

        [System.Diagnostics.Conditional("USE_LOG")]
        public static void LogWarning(string msg)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
            Debug.LogWarning(msg);
#else
            Log.Warning(msg);
#endif
        }

        [System.Diagnostics.Conditional("USE_LOG")]
        public static void LogError(string msg)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
            Debug.LogError(msg);
#else
            Log.Error(msg);
#endif
        }

        [System.Diagnostics.Conditional("USE_LOG")]
        public static void LogDrawRay(Vector3 start, Vector3 dir, Vector3 color, float duration)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
            Debug.DrawRay(start, dir, new Color(color.x, color.y, color.z), duration, false);
#endif
        }

        [System.Diagnostics.Conditional("USE_LOG")]
        public static void LogDrawLine(Vector3 start, Vector3 end, Vector3 color, float duration)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
            Debug.DrawLine(start, end, new Color(color.x, color.y, color.z), duration, false);
#endif
        }

        [System.Diagnostics.Conditional("USE_LOG")]
        public static void LogCallStack(string msg)
        {
            var st = new System.Diagnostics.StackTrace(true);
            string callstack = "";
            foreach (var frame in st.GetFrames())
            {
                callstack += $"filename:{frame.GetFileName()}, line:{frame.GetFileLineNumber()}, function:{frame.GetMethod()}\n";
            }
            LogHelper.LogInfo($"callstack {callstack}, msg {msg}");
        }

    }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
#else
    public static class Debug
    {
        [System.Diagnostics.Conditional("USE_LOG")]
        public static void Log(string msg)
        {
            Serilog.Log.Information(msg);
        }

        [System.Diagnostics.Conditional("USE_LOG")]
        public static void LogError(string msg)
        {
            Serilog.Log.Error(msg);
        }

        [System.Diagnostics.Conditional("USE_LOG")]
        public static void LogWarning(string msg)
        {
            Serilog.Log.Warning(msg);
        }
    }
#endif
}
