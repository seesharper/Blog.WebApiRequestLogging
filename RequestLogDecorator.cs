namespace WebApiRequestLogging
{
    using System;

    public class RequestLogDecorator : ILog
    {
        private readonly ILog log;
        private readonly Func<RequestInfo> getRequestInfo;

        public RequestLogDecorator(ILog log, Func<RequestInfo> getRequestInfo)
        {
            this.log = log;
            this.getRequestInfo = getRequestInfo;
        }

        public void Info(string message)
        {
            log.Info($"Request id: {getRequestInfo().Id} {message}");
        }

        public void Debug(string message)
        {
            log.Debug($"Request id: {getRequestInfo().Id} {message}");
        }

        public void Error(string message, Exception exception = null)
        {
            log.Error($"Request id: {getRequestInfo().Id} {message}", exception);
        }
    }
}