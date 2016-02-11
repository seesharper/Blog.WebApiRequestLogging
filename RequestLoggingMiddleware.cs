namespace WebApiRequestLogging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Eventing.Reader;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    public class RequestLoggingMiddleware : OwinMiddleware
    {
        private readonly ILog log;


        public RequestLoggingMiddleware(OwinMiddleware next, ILog log) : base(next)
        {
            this.log = log;
        }

        public override async Task Invoke(IOwinContext context)
        {            
            await Measure(context);            
        }

        private async Task Measure(IOwinContext context)
        {
            var stopWath = Stopwatch.StartNew();
            await Next.Invoke(context).ConfigureAwait(false);
            stopWath.Stop();
            log.Info($"Request {context.Request.Uri.PathAndQuery} took {stopWath.ElapsedMilliseconds} ms");
        }
    }
}