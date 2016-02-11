namespace WebApiRequestLogging
{
    using System;

    /// <summary>
    ///     Represents a logging class.
    /// </summary>
    public interface ILog
    {      
        void Info(string message);

        void Debug(string message);
    
        void Error(string message, Exception exception = null);
    }
}