using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Configuration;

namespace demo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .Configure(app => app.UseServiceStack(new AppHost()))
                .Build();
    }

    public class AppHost : AppHostBase
    {
        public AppHost(): base("DemoApp", typeof(AppHost).Assembly)
        {
        }

        public override void Configure(Container container)
        {
        }
    }

    public class RestartService: Service
    {
        public IApplicationLifetime Lifetime { get; set; }

        public void Any(RestartRequest request)
        {
            Lifetime.StopApplication();
        }

        static readonly string RuntimeId = Guid.NewGuid().ToString("N");
        public object Any(FallbackRoute request)
        {
            return new {
                Message = "The RuntimeId will change everytime the application is restarted",
                RuntimeId = RuntimeId,
                Restart = base.Request.ResolveAbsoluteUrl("~/restart")
            };
        }
    }

    [Route("/restart")]
    public class RestartRequest: IReturnVoid {}

    [FallbackRoute("/{Path*}")]
    public class FallbackRoute
    {
        public string Path { get; set; }
    }
}
