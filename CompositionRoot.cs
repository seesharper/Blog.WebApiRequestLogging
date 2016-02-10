namespace WebApiRequestLogging
{
    using LightInject;
    public class CompositionRoot : ICompositionRoot
    {
        public void Compose(IServiceRegistry serviceRegistry)
        {
            serviceRegistry.Register<ILogFactory, Log4NetLogFactory>(new PerContainerLifetime());
            serviceRegistry.RegisterConstructorDependency((factory, info) => factory.GetInstance<ILogFactory>().GetLogger(info.Member.DeclaringType));
        }
    }
}