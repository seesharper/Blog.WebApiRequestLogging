namespace WebApiRequestLogging
{
    using System;

    public interface ILogFactory
    {
        ILog GetLogger(Type type);
    }
}