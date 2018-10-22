using System;
using System.Reflection;

namespace SfdcDataSync.Core
{
    public class Logger
    {
        public delegate void Log(string message);
        public delegate void LogException(string message, Exception exception);

        private static Log _debug;
        private static LogException _debugException;
        private static Log _info;
        private static LogException _infoException;
        private static Log _warn;
        private static LogException _warnException;
        private static Log _error;
        private static LogException _errorException;
        private static Log _fatal;
        private static LogException _fatalException;
        private static string _outputPath;

        public static void SetLogger(
            Log debug, LogException debugException,
            Log info, LogException infoException,
            Log warn, LogException warnException,
            Log error, LogException errorException,
            Log fatal, LogException fatalException,
            string outputPath = "")
        {
            _debug = debug ?? throw new ArgumentNullException(nameof(debug));
            _debugException = debugException ?? throw new ArgumentNullException(nameof(debug));
            _info = info ?? throw new ArgumentNullException(nameof(info));
            _infoException = infoException ?? throw new ArgumentNullException(nameof(info));
            _warn = warn ?? throw new ArgumentNullException(nameof(warn));
            _warnException = warnException ?? throw new ArgumentNullException(nameof(warn));
            _error = error ?? throw new ArgumentNullException(nameof(error));
            _errorException = errorException ?? throw new ArgumentNullException(nameof(error));
            _fatal = fatal ?? throw new ArgumentNullException(nameof(fatal));
            _fatalException = fatalException ?? throw new ArgumentNullException(nameof(fatal));
            _outputPath = string.IsNullOrWhiteSpace(outputPath)
                ? System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                : outputPath;
        }

        internal static string OutputPath => _outputPath;

        internal static void LogDebug(string message)
        {
            _debug(message);
        }

        internal static void LogDebugException(string message, Exception exception)
        {
            _debugException(message, exception);
        }

        internal static void LogInfo(string message)
        {
            _info(message);
        }

        internal static void LogInfoException(string message, Exception exception)
        {
            _infoException(message, exception);
        }

        internal static void LogWarn(string message)
        {
            _warn(message);
        }

        internal static void LogWarnException(string message, Exception exception)
        {
            _warnException(message, exception);
        }

        internal static void LogError(string message)
        {
            _error(message);
        }

        internal static void LogErrorException(string message, Exception exception)
        {
            _errorException(message, exception);
        }

        internal static void LogFatal(string message)
        {
            _fatal(message);
        }

        internal static void LogFatalException(string message, Exception exception)
        {
            _fatalException(message, exception);
        }
    }
}
