namespace WebApiRequestLogging
{
    using System;
    using log4net;
    using log4net.Config;

    public class Log4NetLogFactory : ILogFactory
    {
        public Log4NetLogFactory()
        {
            XmlConfigurator.Configure();           
        }

        public ILog GetLogger(Type type)
        {            
            var logger = LogManager.GetLogger(type);            
            return new Log(logger.Info, logger.Debug, logger.Error);
        }
    }
}