namespace WebApiRequestLogging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    public class RequestContextMiddleware : OwinMiddleware
    {
        private static readonly AsyncLocal<RequestContext> RequestContextStorage = new AsyncLocal<RequestContext>();

        public RequestContextMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            RequestContextStorage.Value = new RequestContext(Guid.NewGuid().ToString());
            await Next.Invoke(context);
        }

        public static RequestContext CurrentRequest => RequestContextStorage.Value;
    }
}