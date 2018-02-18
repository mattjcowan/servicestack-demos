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
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace demo {
    public class Program {
        public static void Main (string[] args) {
            BuildWebHost (args).Run ();
        }

        public static IWebHost BuildWebHost (string[] args) =>
            WebHost.CreateDefaultBuilder (args)
            .Configure (app => app.UseServiceStack (new AppHost (app.ApplicationServices.GetRequiredService<IHostingEnvironment>())))
            .Build ();
    }

    public class AppHost : AppHostBase {
        public string RootDir { get; }
        public string DataDir { get; }

        public AppHost (IHostingEnvironment env) : base (env.ApplicationName, typeof (AppHost).Assembly) { 
            this.RootDir = env.ContentRootPath;
            this.DataDir = Path.Combine(this.RootDir, "App_Data");
            Directory.CreateDirectory(this.DataDir);
        }

        public override void Configure (Container container) {

            var dbFile = Path.Combine(this.DataDir, "db.json");
            if (File.Exists(dbFile)) 
            {
                var dbConnectionInfo = File.ReadAllText(dbFile).FromJson<DbConnectionInfo>();
                var dbConnectionFactory = GetDbConnectionFactory(dbConnectionInfo);
                this.Register<IDbConnectionFactory>(dbConnectionFactory);
            }

            if (this.TryResolve<IDbConnectionFactory>() == null) 
            {
                GlobalRequestFiltersAsync.Add(async (req, res, requestDto) => {
                    if (req.ResponseContentType == MimeTypes.Html &&
                        req.Verb.EqualsIgnoreCase("GET") &&                     
                        !req.PathInfo.StartsWithIgnoreCase("/api") &&        
                        !req.PathInfo.StartsWithIgnoreCase("/install") &&
                        !req.PathInfo.StartsWithIgnoreCase("/pages"))
                    {
                        var dbFactory = req.TryResolve<IDbConnectionFactory>();
                        if (dbFactory == null)
                        {
                            res.ContentType = MimeTypes.Html;
                            await res.WriteAsync(LoadPage("install/index.html"));
                            await res.EndRequestAsync();
                        }
                    }
                });
            }
        }

        public static OrmLiteConnectionFactory GetDbConnectionFactory(DbConnectionInfo db)
        {
            if (string.IsNullOrWhiteSpace(db.Dialect) || string.IsNullOrWhiteSpace(db.ConnectionString))
                return null;

            IOrmLiteDialectProvider dialectProvider = null;
            var dialect = db.Dialect.ToLowerInvariant();
            var connectionString = db.ConnectionString;

            if (dialect.Contains("sqlite"))
                dialectProvider = SqliteDialect.Provider;
            else if (dialect.Contains("pgsql") || dialect.Contains("postgres"))
                dialectProvider = PostgreSqlDialect.Provider;
            else if (dialect.Contains("mysql"))
                dialectProvider = MySqlDialect.Provider;
            else if (dialect.Contains("sqlserver"))
            {
                if (dialect.Contains("2017"))
                    dialectProvider = SqlServer2017Dialect.Provider;
                else if (dialect.Contains("2016"))
                    dialectProvider = SqlServer2016Dialect.Provider;
                else if (dialect.Contains("2014"))
                    dialectProvider = SqlServer2014Dialect.Provider;
                else if (dialect.Contains("2012"))
                    dialectProvider = SqlServer2012Dialect.Provider;
                else
                    dialectProvider = SqlServerDialect.Provider;
            }

            return (dialectProvider != null) ? new OrmLiteConnectionFactory(connectionString, dialectProvider): (OrmLiteConnectionFactory) null;
        }

        private static string LoadPage (string templateName) {
            var templatePath = "/pages/" + templateName;
            var file = HostContext.VirtualFileSources.GetFile (templatePath);
            if (file == null)
                throw new FileNotFoundException ("Could not load HTML template embedded resource: " + templatePath, templateName);

            var contents = file.ReadAllText ();
            return contents;
        }
    }

    public class DbConnectionInfo
    {
        public string Dialect { get; set; }
        public string ConnectionString { get; set; }
    }

    public class FallbackService : Service { 
        public object Any(FallbackRequest request)
        {
            return HttpError.NotFound("Not Found");
        }
    }

    [FallbackRoute ("/{Path*")]
    public class FallbackRequest {
        public string Path { get; set; }
    }
}