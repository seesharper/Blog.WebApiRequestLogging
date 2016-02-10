using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiRequestLogging
{
    using Topshelf;

    class Program
    {
        private static int Main()
        {
            return (int)HostFactory.Run(
                 host =>
                 {
                     host.Service<WebApplication>(
                         service =>
                         {
                             service.ConstructUsing(() => new WebApplication());
                             service.WhenStarted(s => s.Start());
                             service.WhenStopped(s => s.Stop());
                         });
                     host.RunAsLocalSystem();
                     host.SetServiceName("WebApiRequestLogging");
                     host.SetDisplayName("WebApiRequestLogging");
                     host.SetDescription("WebApiRequestLogging Web Server");
                 });
        }
    }
}
