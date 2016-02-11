namespace WebApiRequestLogging
{
    using System;
    using LightInject;
    public class CompositionRoot : ICompositionRoot
    {
        public void Compose(IServiceRegistry serviceRegistry)
        {            
            serviceRegistry.Register<ILogFactory, Log4NetLogFactory>(new PerContainerLifetime());
            serviceRegistry.Register<Type, ILog>((factory, type) => factory.GetInstance<ILogFactory>().GetLogger(type));
            serviceRegistry.RegisterConstructorDependency(
                (factory, info) => factory.GetInstance<Type, ILog>(info.Member.DeclaringType));            
        }
    }
}