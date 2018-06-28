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
            var pathLowerCase = path.ToLower();

            var redirects = new Dictionary<string, string>() { { "/docs", "/docs/" }, { "/api", "/api/" } };
            if (redirects.ContainsKey(pathLowerCase))
            {
                context.HttpContext.Response.Redirect(redirects[pathLowerCase], true);
            }
            else 
            {
                if (pathLowerCase.StartsWith("/docs/"))
                {
                    request.Path = "/swagger-ui/" + path.Substring(6);
                }
                else if (pathLowerCase.Equals("/api/"))
                {
                    request.Path = "/metadata";
                } 
                else if (pathLowerCase.StartsWith("/api/"))
                {
                    request.Path = path.Substring(4);
                }
            }
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost(): base("demo", typeof(AppHost).Assembly) {}
        public override void Configure(Container container)
        {
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
