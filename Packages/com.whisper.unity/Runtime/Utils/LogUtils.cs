using System;
using UnityEngine;

namespace Whisper.Utils
{
    public enum LogLevel
    {
        Verbose,
        Log,
        Warning,
        Error,
    }

    /// <summary>
    /// Wrapper for Unity logger that can be configured by log level.
    /// </summary>
    public static class LogUtils
    {
        public static LogLevel Level = LogLevel.Verbose;
        
        public static void Exception(Exception msg)
        {
            Debug.LogException(msg);
        }
        
        public static void Error(string msg)
        {
            Debug.LogError(msg);
        }

        public static void Warning(string msg)
        {
            if (Level > LogLevel.Warning)
                return;
            Debug.LogWarning(msg);
        }

        public static void Log(string msg)
        {
            if (Level > LogLevel.Log)
                return;
            Debug.Log(msg);
        }
        
        public static void Verbose(string msg)
        {
            if (Level > LogLevel.Verbose)
                return;
            Debug.Log(msg);
        }      
    }
}