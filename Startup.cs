using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(WebApiRequestLogging.Startup))]

namespace WebApiRequestLogging
{
    using System.Net.Http.Formatting;
    using System.Web.Http;
    using LightInject;

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var configuration = new HttpConfiguration();
            ConfigureHttpRoutes(configuration);
            configuration.Formatters.Clear();
            configuration.Formatters.Add(new JsonMediaTypeFormatter());
            var container = new ServiceContainer();
            container.RegisterFrom<CompositionRoot>();
            container.RegisterApiControllers();
            container.EnableWebApi(configuration);
            app.UseWebApi(configuration);
        }

        private static void ConfigureHttpRoutes(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "API Default",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });
        }
    }
}
