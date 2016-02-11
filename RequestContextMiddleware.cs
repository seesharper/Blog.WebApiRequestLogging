namespace WebApiRequestLogging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    public class RequestInfoMiddleware : OwinMiddleware
    {
        private static readonly AsyncLocal<RequestInfo> RequestInfo = new AsyncLocal<RequestInfo>();

        public RequestInfoMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            RequestInfo.Value = new RequestInfo(Guid.NewGuid().ToString());
            await Next.Invoke(context);
        }

        public static RequestInfo CurrentRequest => RequestInfo.Value;
    }
}