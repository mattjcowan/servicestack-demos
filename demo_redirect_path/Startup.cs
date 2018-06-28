using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;

namespace demo
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseDefaultFiles();

            app.UseRewriter(new RewriteOptions()
                .Add(RewriteApiRequests));
            app.UseServiceStack(new AppHost());

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        public static void RewriteApiRequests(RewriteContext context)
        {
            var request = context.HttpContext.Request;
            var path = request.Path.Value;
            if (path.Equals("/docs", StringComparison.OrdinalIgnoreCase))
            {
                context.HttpContext.Response.Redirect("/docs/", false);
            } 
            else if (path.Equals("/api", StringComparison.OrdinalIgnoreCase))
            {
                context.HttpContext.Response.Redirect("/api/", false);
            }
            else 
            {
                if (path.StartsWith("/docs/", StringComparison.OrdinalIgnoreCase))
                {
                    request.Path = "/swagger-ui/" + (path.Length == 5 ? "": request.Path.Value.Substring(6));
                }
                else if (path.Equals("/api/", StringComparison.OrdinalIgnoreCase))
                {
                    request.Path = "/metadata";
                } 
                else if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                {
                    request.Path = request.Path.Value.Substring(4);
                }
            }
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost(): base("demo", typeof(AppHost).Assembly) {}
        public override void Configure(Container container)
        {
            SetConfig(new HostConfig {
                // DefaultRedirectPath = "/"
            });
            Plugins.Add(new ServiceStack.Api.OpenApi.OpenApiFeature());
        }
    }

    public class FallbackService: Service
    {
        public object Any(FallbackRequest request)
        {
            return new { 
                Hello = "ServiceStack", 
                Path = "/" + (request.PathInfo ?? "") 
            };
        }
    }


    [FallbackRoute("/{PathInfo*}")]
    public class FallbackRequest
    {
        public string PathInfo { get; set; }
    }
}
