namespace WebApiRequestLogging
{
    using System;

    /// <summary>
    ///     An <see cref="ILog" /> implementation that uses Log4Net to
    ///     perform the actual logging.
    /// </summary>
    public class Log : ILog
    {
        private readonly Action<string> logDebug;
        private readonly Action<string, Exception> logError;
        private readonly Action<string> logInfo;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Log" /> class.
        /// </summary>
        /// <param name="logInfo">The delegate to invoke when logging an info message.</param>
        /// <param name="logDebug">The delegate to invoke when logging a debug message.</param>
        /// <param name="logError">The delegate to invoke when logging an error message.</param>
        public Log(Action<string> logInfo, Action<string> logDebug, Action<string, Exception> logError)
        {
            this.logInfo = logInfo;
            this.logDebug = logDebug;
            this.logError = logError;
        }

        /// <summary>
        ///     Writes an informational message to the log.
        /// </summary>
        /// <param name="message">The message to be written to the log.</param>
        public void Info(string message)
        {
            logInfo(message);
        }

        /// <summary>
        ///     Writes a debug message to the log.
        /// </summary>
        /// <param name="message">The message to be written to the log.</param>
        public void Debug(string message)
        {
            logDebug(message);
        }

        /// <summary>
        ///     Writes an error message to the log.
        /// </summary>
        /// <param name="message">The message to be written to the log.</param>
        /// <param name="exception">Optionally the <see cref="Exception" /> that caused the error.</param>
        public void Error(string message, Exception exception = null)
        {
            logError(message, exception);
        }
    }
}