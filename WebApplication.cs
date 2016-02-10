namespace WebApiRequestLogging
{
    using System;
    using Microsoft.Owin.Hosting;

    public class WebApplication
    {
        private IDisposable m_application;

        public void Start()
        {
            m_application = WebApp.Start<Startup>("http://localhost:8080");
        }

        public void Stop()
        {
            m_application.Dispose();
        }
    }
}