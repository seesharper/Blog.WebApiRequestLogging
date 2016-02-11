# Web Api Request Logging

This post is going to show how to use LightInject to enable logging in a Web Api application. We are going to look how to preserve contextual information associated with the incoming request so that this information can be used for logging purposes. All this goodness is going to end up in a simple console application that shows how all the pieces fit together.



## Logging 

Since logging is a cross cutting concern and is to be found scatted all around in our application, it makes sense to create an abstraction so that we don't create a direct dependency on a particular logging framework. This abstraction is something that we should own rather than relying on third part abstraction such as Common Logging. 

So we start of with a simple interface that is going to be used for logging.

```
public interface ILog
{      
    void Info(string message);

    void Debug(string message);

    void Error(string message, Exception exception = null);
}
```

This is the interface that we will be injection into controllers, services or any other class that requires logging.

The actual implementation of this interface looks like this

```
public class Log : ILog
{
    private readonly Action<string> logDebug;
    private readonly Action<string, Exception> logError;
    private readonly Action<string> logInfo;

    public Log(Action<string> logInfo, Action<string> logDebug, Action<string, Exception> logError)
    {
        this.logInfo = logInfo;
        this.logDebug = logDebug;
        this.logError = logError;
    }

    public void Info(string message)
    {
        logInfo(message);
    }

    public void Debug(string message)
    {
        logDebug(message);
    }

    public void Error(string message, Exception exception = null)
    {
        logError(message, exception);
    }
}
```

The **Log** class is not tied to a specific logging framework and it just takes a set of action delegates that represents the three logging levels supported by our abstraction.

This simple application has just one controller named **PingController** that is simply going to return the text "pong".

```
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
        var result =  Ok("Pong");
        log.Info("Ping end");
        return result;
    }
}
```

As we can see we are injecting ILog into the controller and this is where LightInject comes into play.

## Enabling dependency injection 

First step is to install the **LightInject.WebApi** package. This package integrates **LightInject** with Web Api and makes it very simple to do dependency injection with Web Api controllers.

The startup class looks like this

```
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
```

In addition to this we have the composition root where we register services with the container. 

```
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
```

The first service that we register is the **ILogFactory** that is responsible for creating an ILog instance based on a given type.

> It is common to create a logger that is associated with the type where the logger is used.

This interface looks like this:

```
public interface ILogFactory
{
    ILog GetLogger(Type type);
}
```

And since we are going to be using **Log4Net** in this sample application, we have an implementation called **Log4NetLogFactory**.
 
```
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
``` 

Getting back to the **CompositionRoot** class we still have one line of code that might need some further explanation.
```
serviceRegistry.RegisterConstructorDependency((factory, info) => factory.GetInstance<ILogFactory>().GetLogger(info.Member.DeclaringType));
```

This tells the container that whenever it sees an **ILog** constructor dependency, it should execute the given factory delegate passing the ParameterInfo into the delegate. This again makes it possible for us to create an **ILog** based on the actual class that uses the **ILog** instance.  
 
That is pretty much it, press F5 run run the app and enter the following url in your favorite browser.

```
http://localhost:8080/api/ping
```

That should yield the following output in the console

```
2016-02-11 09:14:28,489 [INFO] 13 WebApiRequestLogging.PingController: Ping start
2016-02-11 09:14:28,603 [INFO] 6 WebApiRequestLogging.PingController: Ping end
```

## Request logging

Next task is to enable some sort of logging of each web request and also add some contextual information such as the request url and a token that makes it possible to associate all log entries with a particular request.

We start off with a simple class (Owin middleware) that logs the duration of the request.

```
public class RequestLoggingMiddleware : OwinMiddleware
{
    private readonly ILog log;


    public RequestLoggingMiddleware(OwinMiddleware next, ILog log) : base(next)
    {
        this.log = log;
    }

    public override async Task Invoke(IOwinContext context)
    {            
        await Measure(context);            
    }

    private async Task Measure(IOwinContext context)
    {
        var stopWath = Stopwatch.StartNew();
        await Next.Invoke(context).ConfigureAwait(false);
        stopWath.Stop();
        log.Info($"Request {context.Request.Uri.PathAndQuery} took {stopWath.ElapsedMilliseconds} ms");
    }
}
```

In addition to this we need to add this new middleware to the Owin pipeline by adding this line to the **CompositionRoot** class.

```
app.Use<RequestLoggingMiddleware>(container.GetInstance<ILogFactory>().GetLogger(typeof(RequestLoggingMiddleware)));
``` 

Console output should now be

```
2016-02-11 13:07:48,466 [INFO] 11 WebApiRequestLogging.RequestLoggingMiddleware: Request /api/ping took 4 ms
```

## Request Context

In some situations it is useful to be able to associate all log entries with the current web request. This can be used for analyzing the log later in tools such as Splunk making it possible to see all log entries tied to any given request.

We could make the **IOwinContext** available in the container so that it could be injected into any class that requires information about the current request. This would however mean that these classes would have to know about the **IOwinContext** which might not be the best solution. 

Lets start off simple by creating a class to hold the request identifier.

```
public class RequestContext
{
    public RequestContext(string id)
    {
        Id = id;
    }

    public string Id { get; } 
}
```

Next we create another middleware class to set the request identifier.

```
public class RequestContextMiddleware : OwinMiddleware
{
    private static readonly AsyncLocal<RequestContext> RequestInfo = new AsyncLocal<RequestContext>();

    public RequestContextMiddleware(OwinMiddleware next) : base(next)
    {
    }

    public override async Task Invoke(IOwinContext context)
    {
        RequestInfo.Value = new RequestContext(Guid.NewGuid().ToString());
        await Next.Invoke(context);
    }

    public static RequestContext CurrentRequest => RequestInfo.Value;
}
```

The actual **RequestContext** uses the **AsyncLocal&lt;T&gt;** class to ensure that the context flows across await points.

> The **AsyncLocal&lt;T&gt;** class is sort of the async version of **ThreadLocal&lt;T&gt;**. You should NEVER rely on any kind of storage that is tied to a specific thread in an async environment.

Then we need to add the  **RequestContextMiddleware** to the Owin pipeline.

```
 app.Use<RequestContextMiddleware>();
```



> Written with [StackEdit](https://stackedit.io/).




