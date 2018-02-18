using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using demo;
using Microsoft.AspNetCore.Hosting;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace demo
{
    public class InstallService: Service
    {
        public IApplicationLifetime Lifetime { get; set; }
        
        public object Any(DbConnectionRequest request)
        {
            ValidateAccessRights();
            if (string.IsNullOrWhiteSpace(request.ConnectionString))
                throw new ArgumentNullException(nameof(request.ConnectionString));
            if (string.IsNullOrWhiteSpace(request.Dialect))
                throw new ArgumentNullException(nameof(request.Dialect));

            ValidateDbConnection(request);
            PersistDbConfig(request);
            
            return new { success = true };
        }

        public object Any(RestartRequest request)
        {
            Lifetime.StopApplication();
            return new { success = true };
        }

        public object Any(PingRequest request)
        {
            return new { serverTime = DateTime.UtcNow };
        }

        private void ValidateAccessRights()
        {
            if (Request.TryResolve<IDbConnectionFactory>() == null ||
                Request.GetSession().HasRole("Admin", null))
            {
                // all good
                return;
            }

            throw HttpError.Forbidden("Forbidden");
        }

        private static readonly object FileLock = new object();

        private void ValidateDbConnection(DbConnectionInfo dbInfo)
        {
            try
            {
                using (AppHost.GetDbConnectionFactory(dbInfo).OpenDbConnection())
                {
                    // sweet!
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        private static string GetDbConfigFilePath()
        {
            return Path.Combine((AppHost.Instance as AppHost).DataDir, "db.json");
        }

        private static void PersistDbConfig(DbConnectionInfo dbConfig)
        {
            var dbConfigFile = GetDbConfigFilePath();
            lock (FileLock)
            {
                int i = 0; // try up to 10 times
                while (i <= 10)
                {
                    try
                    {
                        File.WriteAllText(dbConfigFile, dbConfig.ToJson());
                        break;
                    }
                    catch
                    {
                        i++;
                    }
                }
            }
        }
    }
    
    [Route("/api/db", "POST, PUT")]
    public class DbConnectionRequest : DbConnectionInfo
    {
    }
    
    [Route("/api/restart")]
    public class RestartRequest
    {
    }
    
    [Route("/api/ping")]
    public class PingRequest
    {
    }
}