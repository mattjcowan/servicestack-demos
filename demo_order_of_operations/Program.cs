using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using Funq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.Metadata;
using ServiceStack.Redis;
using ServiceStack.Web;

namespace demo {
    public class Program {
        public static void Main (string[] args) {
            BuildWebHost (args).Run ();
        }

        public static IWebHost BuildWebHost (string[] args) =>
            WebHost.CreateDefaultBuilder (args)
            .Configure (app => app.UseServiceStack (new AppHost (app.ApplicationServices)))
            .Build ();
    }

    public class OrderOfOperations {
        internal static string Log = null;
        public static Lazy<OrderOfOperations> Instance = new Lazy<OrderOfOperations> (() => new OrderOfOperations ());
        private OrderOfOperations () { }
        public static void Add (Type type, string methodOrProperty) {
            var operation = new Operation { Type = type, MethodOrProperty = methodOrProperty };
            Instance.Value.Operations.Enqueue (operation);
            if (!string.IsNullOrWhiteSpace(Log))
                File.AppendAllLines(Log, new [] { operation.ToJsv() });
        }

        public static List<Operation> GetOperations () {
            var i = 0;
            var d = new List<Operation> ();
            foreach (var op in OrderOfOperations.Instance.Value.Operations) {
                op.Id = ++i;
                d.Add (op);
            }
            return d;
        }

        public static void Clear()
        {
            OrderOfOperations.Instance.Value.Operations.Clear();
        }

        private ConcurrentQueue<Operation> Operations = new ConcurrentQueue<Operation> ();

        public class Operation {
            public int Id { get; set; }
            public Type Type { get; set; }
            public string MethodOrProperty { get; set; }
        }
    }

    public class AppHost : AppHostBase {
        public AppHost (IServiceProvider services):
            base (services.GetRequiredService<IHostingEnvironment> ().ApplicationName, typeof (AppHost).Assembly) {
                OrderOfOperations.Log = Path.Combine(services.GetRequiredService<IHostingEnvironment>().ContentRootPath, "orderofoperations.txt");
                if (File.Exists(OrderOfOperations.Log)) File.WriteAllText(OrderOfOperations.Log, "");

                this.AppSettings = new NetCoreAppSettings (services.GetRequiredService<IConfiguration> ());
                OrderOfOperations.Add (typeof (AppHost), "() // Constructor");

                this.RawHttpHandlers.Add ((req) => {
                    OrderOfOperations.Add (typeof (AppHost), "RawHttpHandlers[]");
                    return null;
                });

                this.CatchAllHandlers.Add ((httpMethod, pathInfo, filePath) => {
                    OrderOfOperations.Add (typeof (AppHost), "CatchAllHandlers[]");
                    return null;
                });

                this.PreRequestFilters.Add ((req, res) => {
                    OrderOfOperations.Add (typeof (AppHost), "PreRequestFilters");
                });

                this.GlobalRequestFilters.Add((req, res, dto) => {
                    OrderOfOperations.Add (typeof (AppHost), "GlobalRequestFilters");
                });
            }

        public override Task ProcessRequest (HttpContext context, Func<Task> next) {
            OrderOfOperations.Add (typeof (AppHost), "ProcessRequest()");
            return base.ProcessRequest (context, next);
        }

        public override void Bind (IApplicationBuilder app) {
            OrderOfOperations.Add (typeof (AppHost), "Bind()");
            base.Bind (app);
        }

        public override ServiceStackHost Init () {
            OrderOfOperations.Add (typeof (AppHost), "Init()");
            return base.Init ();
        }

        public override void Configure (Container container) {
            SetConfig (new HostConfig ());
            OrderOfOperations.Add (typeof (AppHost), "Configure()");

            base.RequestBinders.Add (typeof (FallbackRequest), req => {
                OrderOfOperations.Add (typeof (AppHost), "RequestBinders");
                return req.Dto;
            });

            base.RequestBinders.Add (typeof (ClearRequest), req => {
                OrderOfOperations.Add (typeof (AppHost), "RequestBinders");
                return req.Dto;
            });

            Plugins.Add (new AddedPlugin ());
            LoadPlugin (new LoadedPlugin ());

            OrderOfOperations.Add (typeof (AppHost), "Configure() // done configuring");
        }

        public override bool AllowSetCookie (IRequest req, string cookieName) { OrderOfOperations.Add (typeof (AppHost), "AllowSetCookie"); return base.AllowSetCookie (req, cookieName); }
        public override void ApplyPreAuthenticateFilters (IRequest httpReq, IResponse httpRes) { OrderOfOperations.Add (typeof (AppHost), "ApplyPreAuthenticateFilters"); base.ApplyPreAuthenticateFilters (httpReq, httpRes); }
        public override IServiceRunner<TRequest> CreateServiceRunner<TRequest> (ActionContext actionContext) { 
            OrderOfOperations.Add (typeof (AppHost), "CreateServiceRunner"); 
            // return new CustomServiceRunner<TRequest>(this, actionContext); 
            return base.CreateServiceRunner<TRequest> (actionContext); 
        }
        public override object ExecuteMessage (IMessage mqMessage) { OrderOfOperations.Add (typeof (AppHost), "ExecuteMessage"); return base.ExecuteMessage (mqMessage); }
        public override object ExecuteMessage (IMessage dto, IRequest req) { OrderOfOperations.Add (typeof (AppHost), "ExecuteMessage"); return base.ExecuteMessage (dto, req); }
        public override object ExecuteService (object requestDto) { OrderOfOperations.Add (typeof (AppHost), "ExecuteService"); return base.ExecuteService (requestDto); }
        public override object ExecuteService (object requestDto, IRequest req) { OrderOfOperations.Add (typeof (AppHost), "ExecuteService"); return base.ExecuteService (requestDto, req); }
        public override object ExecuteService (object requestDto, RequestAttributes requestAttributes) { OrderOfOperations.Add (typeof (AppHost), "ExecuteService"); return base.ExecuteService (requestDto, requestAttributes); }
        public override Task<object> ExecuteServiceAsync (object requestDto, IRequest req) { OrderOfOperations.Add (typeof (AppHost), "ExecuteServiceAsync"); return base.ExecuteServiceAsync (requestDto, req); }
        public override string GenerateWsdl (WsdlTemplateBase wsdlTemplate) { OrderOfOperations.Add (typeof (AppHost), "GenerateWsdl"); return base.GenerateWsdl (wsdlTemplate); }
        public override IAuthRepository GetAuthRepository (IRequest req = null) { OrderOfOperations.Add (typeof (AppHost), "GetAuthRepository"); return base.GetAuthRepository (req); }
        public override string GetBaseUrl (IRequest httpReq) { OrderOfOperations.Add (typeof (AppHost), "GetBaseUrl"); return base.GetBaseUrl (httpReq); }
        public override ICacheClient GetCacheClient (IRequest req) { OrderOfOperations.Add (typeof (AppHost), "GetCacheClient"); return base.GetCacheClient (req); }
        public override ICookies GetCookies (IHttpResponse res) { OrderOfOperations.Add (typeof (AppHost), "GetCookies"); return base.GetCookies (res); }
        public override IDbConnection GetDbConnection (IRequest req = null) { OrderOfOperations.Add (typeof (AppHost), "GetDbConnection"); return base.GetDbConnection (req); }
        public override TimeSpan GetDefaultSessionExpiry (IRequest req) { OrderOfOperations.Add (typeof (AppHost), "GetDefaultSessionExpiry"); return base.GetDefaultSessionExpiry (req); }
        public override MemoryCacheClient GetMemoryCacheClient (IRequest req) { OrderOfOperations.Add (typeof (AppHost), "GetMemoryCacheClient"); return base.GetMemoryCacheClient (req); }
        public override IMessageProducer GetMessageProducer (IRequest req = null) { OrderOfOperations.Add (typeof (AppHost), "GetMessageProducer"); return base.GetMessageProducer (req); }
        public override IRedisClient GetRedisClient (IRequest req = null) { OrderOfOperations.Add (typeof (AppHost), "GetRedisClient"); return base.GetRedisClient (req); }
        public override RouteAttribute[] GetRouteAttributes (Type requestType) { OrderOfOperations.Add (typeof (AppHost), "GetRouteAttributes"); return base.GetRouteAttributes (requestType); }
        public override T GetRuntimeConfig<T> (IRequest req, string name, T defaultValue) { OrderOfOperations.Add (typeof (AppHost), "GetRuntimeConfig<T> --> typeof({0})".FormatWith(typeof(T))); return base.GetRuntimeConfig (req, name, defaultValue); }
        public override IServiceGateway GetServiceGateway (IRequest req) { OrderOfOperations.Add (typeof (AppHost), "GetServiceGateway"); return base.GetServiceGateway (req); }
        public override MetadataTypesConfig GetTypesConfigForMetadata (IRequest req) { OrderOfOperations.Add (typeof (AppHost), "GetTypesConfigForMetadata"); return base.GetTypesConfigForMetadata (req); }
        public override List<IVirtualPathProvider> GetVirtualFileSources () { OrderOfOperations.Add (typeof (AppHost), "GetVirtualFileSources"); return base.GetVirtualFileSources (); }

        public override string GetWebRootPath () {
            OrderOfOperations.Add (typeof (AppHost), "GetWebRootPath()");
            return base.GetWebRootPath ();
        }

        public override Task HandleUncaughtException (IRequest httpReq, IResponse httpRes, string operationName, Exception ex) { OrderOfOperations.Add (typeof (AppHost), "HandleUncaughtException"); return base.HandleUncaughtException (httpReq, httpRes, operationName, ex); }
        public override void LoadPlugin (params IPlugin[] plugins) { OrderOfOperations.Add (typeof (AppHost), "LoadPlugin"); base.LoadPlugin (plugins); }
        public override string MapProjectPath (string relativePath) {
            OrderOfOperations.Add (typeof (AppHost), "MapProjectPath()");
            return base.MapProjectPath (relativePath);
        }
        public override void OnAfterConfigChanged () { OrderOfOperations.Add (typeof (AppHost), "OnAfterConfigChanged"); base.OnAfterConfigChanged (); }
        public override object OnAfterExecute (IRequest req, object requestDto, object response) { OrderOfOperations.Add (typeof (AppHost), "OnAfterExecute"); return base.OnAfterExecute (req, requestDto, response); }
        public override void OnAfterInit () { OrderOfOperations.Add (typeof (AppHost), "OnAfterInit"); base.OnAfterInit (); }
        public override void OnBeforeInit () { OrderOfOperations.Add (typeof (AppHost), "OnBeforeInit"); base.OnBeforeInit (); }

        public override void OnConfigLoad () {
            OrderOfOperations.Add (typeof (AppHost), "OnConfigLoad()");
            base.OnConfigLoad ();
        }
        public override void OnEndRequest (IRequest request = null) { 
            OrderOfOperations.Add (typeof (AppHost), "OnEndRequest -- {0}".FormatWith(request.Dto != null ? request.Dto.GetType().ToString(): "<null>"));
            OrderOfOperations.Add (typeof (AppHost), "------ END OF LAST REQUEST -----");
            base.OnEndRequest (request); 
        }
        public override void OnExceptionTypeFilter (Exception ex, ResponseStatus responseStatus) { OrderOfOperations.Add (typeof (AppHost), "OnExceptionTypeFilter"); base.OnExceptionTypeFilter (ex, responseStatus); }
        public override void OnLogError (Type type, string message, Exception innerEx = null) { OrderOfOperations.Add (typeof (AppHost), "OnLogError"); base.OnLogError (type, message, innerEx); }
        public override object OnPostExecuteServiceFilter (IService service, object response, IRequest httpReq, IResponse httpRes) { OrderOfOperations.Add (typeof (AppHost), "OnPostExecuteServiceFilter"); return base.OnPostExecuteServiceFilter (service, response, httpReq, httpRes); }
        public override object OnPreExecuteServiceFilter (IService service, object request, IRequest httpReq, IResponse httpRes) { OrderOfOperations.Add (typeof (AppHost), "OnPreExecuteServiceFilter"); return base.OnPreExecuteServiceFilter (service, request, httpReq, httpRes); }
        public override void OnSaveSession (IRequest httpReq, IAuthSession session, TimeSpan? expiresIn = null) { OrderOfOperations.Add (typeof (AppHost), "OnSaveSession"); base.OnSaveSession (httpReq, session, expiresIn); }
        public override Task<object> OnServiceException (IRequest httpReq, object request, Exception ex) { OrderOfOperations.Add (typeof (AppHost), "OnServiceException"); return base.OnServiceException(httpReq, request, ex); }
        public override IAuthSession OnSessionFilter (IAuthSession session, string withSessionId) { OrderOfOperations.Add (typeof (AppHost), "OnSessionFilter"); return base.OnSessionFilter(session, withSessionId); }
        public override void OnStartupException (Exception ex) { OrderOfOperations.Add (typeof (AppHost), "OnStartupException"); base.OnStartupException (ex); }
        public override Task OnUncaughtException (IRequest httpReq, IResponse httpRes, string operationName, Exception ex) { OrderOfOperations.Add (typeof (AppHost), "OnUncaughtException"); return base.OnUncaughtException (httpReq, httpRes, operationName, ex); }
        public override void Register<T> (T instance) { OrderOfOperations.Add (typeof (AppHost), "Register<T> --> typeof({0})".FormatWith(typeof(T))); base.Register<T>(instance); }
        public override void RegisterAs<T, TAs>() { OrderOfOperations.Add (typeof (AppHost), "RegisterAs<T, TAs> --> typeof({0})".FormatWith(typeof(T))); base.RegisterAs<T, TAs>(); }
        public override void RegisterService (Type serviceType, params string[] atRestPaths) { OrderOfOperations.Add (typeof (AppHost), "RegisterService"); base.RegisterService(serviceType, atRestPaths); }
        public override void Release (object instance) { OrderOfOperations.Add (typeof (AppHost), "Release"); base.Release(instance); }
        public override T Resolve<T> () { OrderOfOperations.Add (typeof (AppHost), "Resolve<T> --> typeof({0})".FormatWith(typeof(T))); return base.Resolve<T>(); }
        public override string ResolveAbsoluteUrl (string overridePath, IRequest httpReq) { OrderOfOperations.Add (typeof (AppHost), "ResolveAbsoluteUrl"); return base.ResolveAbsoluteUrl(overridePath, httpReq); }
        public override string ResolveLocalizedString (string text, IRequest request) { OrderOfOperations.Add (typeof (AppHost), "ResolveLocalizedString"); return base.ResolveLocalizedString(text, request); }
        public override string ResolvePathInfo (IRequest request, string originalPathInfo) { OrderOfOperations.Add (typeof (AppHost), "ResolvePathInfo({0}, {1})".FormatWith(request.Dto?.GetType(), originalPathInfo)); return base.ResolvePathInfo(request, originalPathInfo); }
        public override string ResolvePhysicalPath (string overridePath, IRequest httpReq) { OrderOfOperations.Add (typeof (AppHost), "ResolvePhysicalPath('{0}', req)".FormatWith(overridePath)); return base.ResolvePhysicalPath(overridePath, httpReq); }
        public override Exception ResolveResponseException (Exception ex) { OrderOfOperations.Add (typeof (AppHost), "ResolveResponseException"); return base.ResolveResponseException(ex); }
        public override IHttpHandler ReturnRedirectHandler (IHttpRequest httpReq) { OrderOfOperations.Add (typeof (AppHost), "ReturnRedirectHandler"); return base.ReturnRedirectHandler(httpReq); }
        public override IHttpHandler ReturnRequestInfoHandler (IHttpRequest httpReq) { OrderOfOperations.Add (typeof (AppHost), "ReturnRequestInfoHandler"); return base.ReturnRequestInfoHandler(httpReq); }
        public override void SetConfig (HostConfig config) { OrderOfOperations.Add (typeof (AppHost), "SetConfig"); base.SetConfig(config); }
        public override bool ShouldCompressFile (IVirtualFile file) { OrderOfOperations.Add (typeof (AppHost), "ShouldCompressFile"); return base.ShouldCompressFile(file); }
        public override ServiceStackHost Start (string urlBase) { OrderOfOperations.Add (typeof (AppHost), "Start"); return base.Start(urlBase); }

        public override IRequest TryGetCurrentRequest () {
            OrderOfOperations.Add (typeof (AppHost), "TryGetCurrentRequest()");
            return base.TryGetCurrentRequest ();
        }

        public override T TryResolve<T> () { OrderOfOperations.Add (typeof (AppHost), "TryResolve<T> --> typeof({0})".FormatWith(typeof(T))); return base.TryResolve<T>(); }
        public override bool UseHttps (IRequest httpReq) { OrderOfOperations.Add (typeof (AppHost), "UseHttps"); return base.UseHttps(httpReq); }
        protected override ServiceController CreateServiceController (params Assembly[] assembliesWithServices) { OrderOfOperations.Add (typeof (AppHost), "CreateServiceController"); return base.CreateServiceController(assembliesWithServices); }

        protected override void Dispose (bool disposing) {
            OrderOfOperations.Add (typeof (AppHost), "Dispose()");
            base.Dispose (disposing);
        }
    }

    internal class CustomServiceRunner<TRequest> : ServiceRunner<TRequest>
    {
        private AppHost appHost;
        private ActionContext actionContext;

        public CustomServiceRunner(AppHost appHost, ActionContext actionContext): base(appHost, actionContext)

        {
            this.appHost = appHost;
            this.actionContext = actionContext;
        }

        public override void OnBeforeExecute(IRequest requestContext, TRequest request) 
        {
            // Called just before any Action is executed
            OrderOfOperations.Add(typeof(CustomServiceRunner<TRequest>), "OnBeforeExecute()");
            base.OnBeforeExecute(requestContext, request);
        }

        public override object OnAfterExecute(IRequest request, object response) 
        {
            // Called just after any Action is executed.
            // You can modify the response returned here as well
            // return base.OnAfterExecute(request, response);
            OrderOfOperations.Add(typeof(CustomServiceRunner<TRequest>), "OnAfterExecute()");
            return OrderOfOperations.GetOperations();
        }
    }

    public class AddedPlugin : IPreInitPlugin, IPlugin, IPostInitPlugin {
        public AddedPlugin () {
            OrderOfOperations.Add (typeof (AddedPlugin), "() // Constructor");
        }

        public void Configure (IAppHost appHost) {
            OrderOfOperations.Add (typeof (AddedPlugin), "Configure(appHost) // IPreInitPlugin");
        }

        public void Register (IAppHost appHost) {
            OrderOfOperations.Add (typeof (AddedPlugin), "Register(appHost) // IPlugin");
        }

        public void AfterPluginsLoaded (IAppHost appHost) {
            OrderOfOperations.Add (typeof (AddedPlugin), "AfterPluginsLoaded(appHost) // IPostInitPlugin");
        }
    }

    public class LoadedPlugin : IPreInitPlugin, IPlugin, IPostInitPlugin {
        public LoadedPlugin () {
            OrderOfOperations.Add (typeof (LoadedPlugin), "() // Constructor");
        }

        public void AfterPluginsLoaded (IAppHost appHost) {
            OrderOfOperations.Add (typeof (LoadedPlugin), "AfterPluginsLoaded(appHost) // IPostInitPlugin");
        }

        public void Configure (IAppHost appHost) {
            OrderOfOperations.Add (typeof (LoadedPlugin), "Configure(appHost) // IPreInitPlugin");
        }

        public void Register (IAppHost appHost) {
            OrderOfOperations.Add (typeof (LoadedPlugin), "Init(appHost) // IPlugin");
        }
    }

    public class FallbackService : Service {
        public FallbackService () {
            OrderOfOperations.Add (typeof (FallbackService), "() // Constructor");
        }

        public object Any (FallbackRequest request) {
            OrderOfOperations.Add (typeof (FallbackService), "Any(FallbackRequest)");
            return OrderOfOperations.GetOperations ();
        }
    }

    [Restrict (VisibilityTo = RequestAttributes.None)]
    [FallbackRoute ("/{Path*}")]
    public class FallbackRequest {
        public FallbackRequest () {
            OrderOfOperations.Add (typeof (FallbackRequest), "() // Constructor");
        }

        public string Path { get; set; }
    }

    public class ClearService : Service {
        public ClearService () {
            OrderOfOperations.Add (typeof (ClearService), "() // Constructor");
        }

        public object Any (ClearRequest request) {
            OrderOfOperations.Clear();
            return OrderOfOperations.GetOperations ();
        }
    }

    public class FreshService : Service {
        public FreshService () {
            OrderOfOperations.Add (typeof (FreshService), "() // Constructor");
        }

        public object Any (FreshRequest request) {
            OrderOfOperations.Add (typeof (FreshService), "Any(FreshRequest) --> START");
            // force some DI calls
            AttemptResolve(() => { var x = base.Db; });
            AttemptResolve(() => { var x = base.Cache; });
            AttemptResolve(() => { var x = base.IsAuthenticated; });
            AttemptResolve(() => { var x = base.VirtualFiles; });
            OrderOfOperations.Add (typeof (FreshService), "Any(FreshRequest) --> END");
            return OrderOfOperations.GetOperations ();
        }

        private void AttemptResolve(Action p)
        {
            try {
                p.Invoke();
            } catch {}
        }
    }

    [Route ("/clear")]
    public class ClearRequest {
        public ClearRequest () {
            OrderOfOperations.Add (typeof (ClearRequest), "() // Constructor");
        }
    }

    [Route ("/fresh")]
    public class FreshRequest {
        public FreshRequest () {
            OrderOfOperations.Add (typeof (FreshRequest), "() // Constructor");
        }
    }
}