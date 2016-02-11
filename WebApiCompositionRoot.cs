namespace WebApiRequestLogging
{
    using System;
    using LightInject;
    public class WebApiCompositionRoot : ICompositionRoot
    {
        public void Compose(IServiceRegistry serviceRegistry)
        {
            serviceRegistry.RegisterFrom<CompositionRoot>();
            serviceRegistry.RegisterApiControllers();
            serviceRegistry.Register<Func<RequestContext>>(factory => (() => RequestContextMiddleware.CurrentRequest), new PerContainerLifetime());
            serviceRegistry.Decorate<ILog, RequestLogDecorator>();
        }
    }
}