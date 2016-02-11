# Web Api Request Logging

This post is going to show you how to use **LightInject** to enable logging in a **Web Api** application. We are going to look into how to preserve contextual information associated with the incoming request so that this information can be used for logging purposes. All this goodness is going to end up in a simple [console application](https://github.com/seesharper/Blog.WebApiRequestLogging) that shows how all the pieces fit together.



## Logging 

Since logging is a cross cutting concern and is to be found scattered all around in our application, it makes sense to create an abstraction so that we don't create a direct dependency on a particular logging framework. This abstraction is something that we should own rather than relying on third part abstraction such as [Common Logging](https://github.com/net-commons/common-logging). Believe me, that is going to cause us nothing but pain as we would have to deal with different versions of a third party abstraction. Own you own abstraction!

We start of with a simple interface that is going to be used for logging.

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

To help us create a **Log** instance, we have this nice little interface.

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

> Note: This is the ONLY place where we actually reference Log4Net.

## Composition root

This application has two composition roots (**ICompositionRoot**), one that registers the core services (**CompositionRoot** )and one that registers services related to **Web Api** (**WebApiCompositionRoot**). 

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

The first service that we register is the **ILogFactory** that is responsible for creating an **ILog** instance based on a given type.

Next, we register the **ILog** service with a factory delegate that calls into the already registered **ILogFactory** service

Finally we tell the container using the **RegisterConstructorDependency** method that whenever it sees an **ILog** constructor dependency, it should provide an **ILog** instance based on the actual class that uses it. 
 
```
public class WebApiCompositionRoot : ICompositionRoot
{
    public void Compose(IServiceRegistry serviceRegistry)
    {
        serviceRegistry.RegisterFrom<CompositionRoot>();
        serviceRegistry.RegisterApiControllers();
    }
}
```

The **WebApiCompositionRoot** registers core services in addition services related to **Web Api** which in this case means the controllers. 


## Controllers


This application has just one controller named **PingController** that is simply going to return the text "pong".

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

As we can see we are injecting an **ILog** instance into the controller.

The **Startup** class for this application looks like this


```
public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var configuration = new HttpConfiguration();
        ConfigureHttpRoutes(configuration);
        ConfigureMediaFormatter(configuration);

        var container = new ServiceContainer();
        container.RegisterFrom<WebApiCompositionRoot>();            
        container.EnableWebApi(configuration);           
                
        app.UseWebApi(configuration);
    }

    private static void ConfigureMediaFormatter(HttpConfiguration configuration)
    {
        configuration.Formatters.Clear();
        configuration.Formatters.Add(new JsonMediaTypeFormatter());
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
 
We can see all this in action just by running the application and hitting the service.

```
http://localhost:8080/api/ping
```

That should yield the following output in the console

```
2016-02-11 09:14:28.489 [INFO] 13 WebApiRequestLogging.PingController: Ping start
2016-02-11 09:14:28.603 [INFO] 6 WebApiRequestLogging.PingController: Ping end
```

The **Log4Net** conversion pattern is like this (app.config)

```
<conversionPattern value="%date{yyyy-MM-dd HH:mm:ss.fff} [%level] %thread %logger: %message%newline" />
```


## Request logging

Sometimes it might be useful to log each request and maybe also the duration of the request.
We start off with a simple class (**OwinMiddleware**) that logs the duration of the request.

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
        await Measure(context).ConfigureAwait(false);;            
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

In addition to this we need to add this new middleware to the **Owin** pipeline by adding this line to the **Startup** class.

```
app.Use<RequestLoggingMiddleware>(container.GetInstance<Type, ILog>(typeof (RequestLogDecorator)));
``` 

> Note: The reason for using an **OwinMiddleware** instead of a **DelegatingHandler** is that the **OwinMiddleware** is not tied to **Web Api** in any way and can also be used in other frameworks that build upon the **Owin** stack.


Console output should now be

```
2016-02-11 13:07:48.466 [INFO] 11 WebApiRequestLogging.RequestLoggingMiddleware: Request /api/ping took 4 ms
```

## Request Context

In some situations it is useful to be able to associate all log entries with the current web request. This can be used for analyzing the log later in tools such as **Splunk** making it possible to see all log entries tied to any given request.

We could make the **IOwinContext** available in the container so that it could be injected into any class that requires information about the current request. This would however mean that these classes would have to know about the **IOwinContext** which might not be the best solution. 

So let's start off simple by creating a class to hold the request identifier.

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
    private static readonly AsyncLocal<RequestContext> RequestContextStorage = new AsyncLocal<RequestContext>();

    public RequestContextMiddleware(OwinMiddleware next) : base(next)
    {
    }

    public override async Task Invoke(IOwinContext context)
    {
        RequestContextStorage.Value = new RequestContext(Guid.NewGuid().ToString());
        await Next.Invoke(context);
    }

    public static RequestContext CurrentRequest => RequestContextStorage.Value;
}
```

The actual **RequestContext** uses the **AsyncLocal&lt;T&gt;** class to ensure that the context flows across await points.

> The **AsyncLocal&lt;T&gt;** class is sort of the async version of **ThreadLocal&lt;T&gt;**. You should NEVER rely on any kind of storage that is tied to a specific thread in an async environment.

Then we need to add the  **RequestContextMiddleware** to the **Owin** pipeline.

```
 app.Use<RequestContextMiddleware>();
```

We now have way to access the current **RequestContext** through the **CurrentRequest** property. Sweet.

The only thing missing now is to register a function delegate that represent getting the current **RequestContext**.

```
serviceRegistry.Register<Func<RequestContext>>(factory => (() => RequestContextMiddleware.CurrentRequest), new PerContainerLifetime());
```

The reason for injection a function delegate rather than just the **RequestContext** is that it might be used in services such as singletons that outlives the scope of a web request. By injecting the delegate that in turn gives us the **RequestContext**, we can be sure that it is valid.

## Decorators

The requirement here is that if we are logging outside the context of a web request, such as in a unit test, we should just log without the request identifier, but if we log inside a web request (production or integration tests), we should add the request identifier to the message being logged. This is a perfect example of where we can apply the **Decorator Pattern**. This allows us to add new functionality to a service without touching the original implementation. Did I hear "open-closed principle", anyone?

```
public class RequestLogDecorator : ILog
{
    private readonly ILog log;
    private readonly Func<RequestContext> getRequestContext;

    public RequestLogDecorator(ILog log, Func<RequestContext> getRequestContext)
    {
        this.log = log;
        this.getRequestInfo = getRequestInfo;
    }

    public void Info(string message)
    {
        log.Info($"Request id: {getRequestContext().Id} {message}");
    }

    public void Debug(string message)
    {
        log.Debug($"Request id: {getRequestContext().Id} {message}");
    }

    public void Error(string message, Exception exception = null)
    {
        log.Error($"Request id: {getRequestContext().Id} {message}", exception);
    }
}
```

The decorator simply wraps the original **ILog** instance and applies the request identifier now returned from the **getRequestContext** delegate.  Don't you just love the new string interpolation features? 

> Note: If you are new to the decorator pattern, you can think of it as a [Russian Doll](https://en.wikipedia.org/wiki/Matryoshka_doll) where inside there is an exact identical doll wrapped by an outer doll.

Decorators are first-class citizens in **LightInject** and applying a decorator is just a one-liner in the **WepApiCompositionRoot** class.

```
serviceRegistry.Decorate<ILog, RequestLogDecorator>();
```

Since we only apply the decorator in the **WepApiCompositionRoot** class it will only be used in the context of a web request.

To "ensure" that we don't always log on the same thread, we modify the **PingController** to inlude a delay.

```
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
```

Running the application and hitting the service should now yield the following output in the console

```
2016-02-11 20:40:30.994 [INFO] 12 WebApiRequestLogging.PingController: Request id: 91444099-72ad-488f-99d6-ab201f20531e Ping start
2016-02-11 20:40:31.111 [INFO] 6 WebApiRequestLogging.PingController: Request id: 91444099-72ad-488f-99d6-ab201f20531e Ping end
2016-02-11 20:40:31.205 [INFO] 6 WebApiRequestLogging.RequestLogDecorator: Request id: 91444099-72ad-488f-99d6-ab201f20531e Request /api/ping took 463 ms
```

Happy logging!!



