using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Configuration;

namespace demo {
    public class Program {
        public static void Main (string[] args) {
            BuildWebHost (args).Run ();
        }

        public static IWebHost BuildWebHost (string[] args) =>
            WebHost.CreateDefaultBuilder (args)
            .ConfigureServices (services => {
                // Enable Node Services
                services.AddNodeServices ();
            })
            .Configure (app => {
                var provider = new FileExtensionContentTypeProvider ();
                provider.Mappings[".tag"] = "text/html";
                app.UseDefaultFiles ();
                app.UseStaticFiles (new StaticFileOptions (new SharedOptions {
                    FileProvider = new PhysicalFileProvider (Path.Combine (Directory.GetCurrentDirectory (), "wwwroot")),
                        RequestPath = ""
                }) {
                    ContentTypeProvider = provider
                });
                app.UseServiceStack (new AppHost (app.ApplicationServices));
            })
            .Build ();
    }

    public class AppHost : AppHostBase {
        public AppHost (IServiceProvider services):
            base (services.GetRequiredService<IHostingEnvironment> ().ApplicationName, typeof (AppHost).Assembly) {
                this.AppSettings = new NetCoreAppSettings (services.GetRequiredService<IConfiguration> ());
                this.RootDir = services.GetRequiredService<IHostingEnvironment> ().ContentRootPath;
                this.TagsDir = Path.Combine (services.GetRequiredService<IHostingEnvironment> ().WebRootPath, "tags");
            }

        public override void Configure (Container container) { }

        public static readonly string ToDoHtml = @"
<!doctype html>
<html>
  <head>
    <title>Riot todo</title>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <link rel=""stylesheet"" href=""/tags/todo.css"">
    <!--[if lt IE 9]>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/es5-shim/4.0.5/es5-shim.min.js""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/html5shiv/3.7.2/html5shiv.min.js""></script>
    <script>html5.addElements('todo')</script>
    <![endif]-->
  </head>

  <body>

    <todo></todo>

    <script src=""/tags/todo.tag"" type=""riot/tag""></script>
    <script src=""https://rawgit.com/riot/riot/master/riot%2Bcompiler.min.js""></script>
    <script src=""//code.jquery.com/jquery-3.3.1.min.js"" integrity=""sha256-FgpCb/KJQlLNfOu91ta32o/NMZxltwRo8QtmkMRdAu8="" crossorigin=""anonymous""></script>
    <script>
    $.ajaxSetup({ contentType: ""application/json; charset=utf-8"" })
    $.getJSON('/todos.json', function( data ) {
        riot.mount('todo', {
            title: 'This is populated from client',
            items: data
        })
    });
    </script>
  </body>
</html>
        ".Trim ();

        public string RootDir { get; }
        public string TagsDir { get; }
    }

    public class TodosService : Service {
        public static ConcurrentDictionary<string, Todo> todosDictionary = new ConcurrentDictionary<string, Todo> ();

        static TodosService () {
            var todos = new List<Todo> ();
            todos.Add (new Todo { title = "Avoid excessive caffeine", done = true });
            todos.Add (new Todo { title = "Hidden item", hidden = true });
            todos.Add (new Todo { title = "Be less provocative" });
            todos.Add (new Todo { title = "Be nice to people" });
            foreach (var t in todos)
                todosDictionary.TryAdd (t.title, t);
        }

        public object Get (TodosRequest request) {
            return todosDictionary.Values.ToList ();
        }

        public object Post (TodosPostRequest request) {
            todosDictionary.Clear ();
            foreach (var t in request)
                todosDictionary.TryAdd (t.title, t);
            return todosDictionary.Values.ToList ();
        }
    }

    [Route ("/todos", "GET")]
    public class TodosRequest : IReturn<List<Todo>> { }

    [Route ("/todos", "POST")]
    public class TodosPostRequest : List<Todo>, IReturn<List<Todo>> { }

    public class Todo {
        public string title { get; set; }
        public bool done { get; set; }
        public bool hidden { get; set; }
    }

    public class FallbackService : Service {
        public INodeServices NodeServices { get; set; }

        public async Task<object> Any (FallbackRequest request) {
            if (Request.ResponseContentType == MimeTypes.Html) {
                var tagsDir = (AppHost.Instance as AppHost).TagsDir;
                var rootDir = (AppHost.Instance as AppHost).RootDir;
                if (request.Path.EqualsIgnoreCase ("ssr")) {
                    var result = await NodeServices.InvokeAsync<string> (
                        Path.Combine (rootDir, "CompileTag.js"),
                        Path.Combine (tagsDir, "todo.tag"),
                        Gateway.Send (new TodosRequest ()));
                    return new HttpResult (AppHost.ToDoHtml.Replace ("<todo></todo>", result));
                } else
                    return new HttpResult (AppHost.ToDoHtml);
            }
            return request;
        }
    }

    [FallbackRoute ("/{Path*}")]
    public class FallbackRequest {
        public string Path { get; set; }
    }
}