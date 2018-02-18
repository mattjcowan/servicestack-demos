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
                .Configure(app => app.UseServiceStack(new AppHost(app.ApplicationServices)))
                .Build();
    }

    public class AppHost : AppHostBase
    {
        public AppHost(IServiceProvider services):
            base(services.GetRequiredService<IHostingEnvironment>().ApplicationName, typeof(AppHost).Assembly)
        {
            this.AppSettings = new NetCoreAppSettings(services.GetRequiredService<IConfiguration>());
        }

        public override void Configure(Container container)
        {
        }
    }

    public class FallbackService: Service
    {
        static Lazy<FileInfo> IndexFile = new Lazy<FileInfo>(() => new FileInfo("wwwroot/index.html".MapAbsolutePath()));

        public object Any(FallbackRequest request) 
        {
            if (Request.ResponseContentType == MimeTypes.Html)
                return new HttpResult(IndexFile.Value);

            return new { Path = request.Path };
        }
    }

    [Restrict(VisibilityTo = RequestAttributes.None)]
    [FallbackRoute("/{Path*}")]
    public class FallbackRequest
    {
        public string Path { get; set; }
    }
}
