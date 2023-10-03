using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ApplicationInsights_Worker.Repositories
{
    /// <summary>
    /// LoggerBase Class
    /// </summary>
    public class BaseLogger
    {
        public enum Level
        {
            ERROR = 0,
            WARNING = 1,
            INFO = 2,
            VERBOSE = 3
        }

        protected string NowAsString { get { return DateTime.UtcNow.ToString("yyyyMMdd HHmmss.fff"); } }

        // -------- Messages ----------------

        /// <summary>
        /// 0 = ERROR/WARN/INFO/VERBOSE
        /// 1 = Code
        /// 2 = Message
        /// 3 = Datetime
        /// </summary>
        protected const string LOG_FORMAT = "{3} {0} {1:D4}: {2}";

        public void Error(int code, Exception exception)
        {
            if (exception == null) return; // No runtime exception here
            Error(code, exception.ToString());
            ExceptionHandler(Level.ERROR, code, exception);
        }

        public void Error(int code, string message, params object[] parameters)
        {
            WriteLine(Level.ERROR, code, message?.ToString() ?? "", parameters);
        }

        public void Warning(int code, Exception exception)
        {
            if (exception == null) return; // No runtime exception here
            Warning(code, exception?.ToString());
        }

        public void Warning(int code, string message, params object[] parameters)
        {
            WriteLine(Level.WARNING, code, message?.ToString() ?? "", parameters);
        }

        public void Info(string message, params object[] parameters)
        {
            WriteLine(Level.INFO, 0, message?.ToString() ?? "", parameters);
        }

        public void Verbose(string message, params object[] parameters)
        {
            WriteLine(Level.VERBOSE, 0, message?.ToString() ?? "", parameters);
        }

        protected virtual void WriteLine(Level msgType, int msgCode, string message, params object[] parameters)
        {
            string formattedMessage = parameters == null || !parameters.Any() ? message : string.Format(message, parameters);
            Console.WriteLine(LOG_FORMAT, msgType.ToString(), msgCode, formattedMessage, NowAsString);
#if DEBUG
            Debug.WriteLine(LOG_FORMAT, msgType.ToString(), msgCode, formattedMessage, NowAsString);
#endif
        }

        protected virtual void ExceptionHandler(Level msgType, int msgCode, Exception e)
        {
            // Handled by children classes
        }

        internal static BaseLogger GetNew()
        {
            return new BaseLogger();
        }

        public static string GetCurrentMethod([CallerMemberName] string callerName = "")
        {
            return callerName;
        }
    }

}
