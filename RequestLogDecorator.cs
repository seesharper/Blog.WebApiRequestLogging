namespace WebApiRequestLogging
{
    using System;

    public class RequestLogDecorator : ILog
    {
        private readonly ILog log;
        private readonly Func<RequestContext> getRequestContext;

        public RequestLogDecorator(ILog log, Func<RequestContext> getRequestContext)
        {
            this.log = log;
            this.getRequestContext = getRequestContext;
        }

        public void Info(string message)
        {
            log.Info($"Request id: {getRequestContext().Id} {message}");
        }

        public void Debug(string message)
        {
            log.Debug($"Request id: {getRequestContext().Id} {message}");
        }

        public void Error(string message, Exception exception = null)
        {
            log.Error($"Request id: {getRequestContext().Id} {message}", exception);
        }
    }
}