namespace WebApiRequestLogging
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Results;

    public class PingController : ApiController
    {
        private readonly ILog log;

        public PingController(ILog log)
        {
            this.log = log;
        }

        public async Task<IHttpActionResult> Get()
        {
            log.Info("Ping start");

            //ConfigureAwait(false) to say that we don't care about synchronization context.
            await Task.Delay(100).ConfigureAwait(false);

            // We are probably on another thread here
            var result =  Ok("Pong");
            log.Info("Ping end");
            return result;
        }
    }
}