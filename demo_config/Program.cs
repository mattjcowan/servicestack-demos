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
using ServiceStack.Text;

namespace demo {
    public class Program {
        public static void Main (string[] args) {
            BuildWebHost (args).Run ();
        }

        public static IWebHost BuildWebHost (string[] args) =>
            WebHost.CreateDefaultBuilder (args)
            .ConfigureAppConfiguration (c => {
                c.SetBasePath (Directory.GetCurrentDirectory ())
                    .AddEnvironmentVariables ()
                    .AddJsonFile ("servicestack.json"); // put this AFTER the environment variables to override env variables;
            })
            .Configure (app => app.UseServiceStack (new AppHost (app.ApplicationServices)))
            .Build ();
    }

    public class AppHost : AppHostBase {

        public IConfiguration _configuration;

        public AppHost (IServiceProvider services):
            base (services.GetRequiredService<IHostingEnvironment> ().ApplicationName, typeof (AppHost).Assembly) {
                this._configuration = services.GetRequiredService<IConfiguration> ();

                // flatten all settings into the AppSettings object
                this.AppSettings = new MultiAppSettingsBuilder ()
                    .AddDictionarySettings (_configuration.AsEnumerable ().ToDictionary (k => k.Key, v => v.Value))
                    .Build ();

                this.ServiceName = this.AppSettings.Get ("App:Name", this.ServiceName);
            }

        public override void Configure (Container container) {

            // translate a config section into a typed object and add the object to the DI container
            var hostConfigSettings = this._configuration.GetSection ("ServiceStack:HostConfig").Get<HostConfigSettingsExtended>();
            this.Register(hostConfigSettings);

            // configure ServiceStack from the typed config object
            var hostConfig = new HostConfig ().PopulateWithNonDefaultValues (hostConfigSettings.ConvertTo<HostConfigSettings> ());
            if (hostConfigSettings.DefaultDocuments != null) { hostConfig.DefaultDocuments.Clear (); hostConfig.DefaultDocuments.AddRange (hostConfigSettings.DefaultDocuments); }
            if (hostConfigSettings.IgnoreWarningsOnPropertyNames != null) { hostConfig.IgnoreWarningsOnPropertyNames.Clear (); hostConfig.IgnoreWarningsOnPropertyNames.AddRange (hostConfigSettings.IgnoreWarningsOnPropertyNames); }
            if (hostConfigSettings.RazorNamespaces != null) { hostConfig.RazorNamespaces.Clear (); hostConfigSettings.RazorNamespaces.Each (i => hostConfig.RazorNamespaces.Add (i)); }
            if (hostConfigSettings.RedirectPaths != null) { hostConfig.RedirectPaths.Clear (); hostConfigSettings.RedirectPaths.Each (i => hostConfig.RedirectPaths.Add (i.Key, i.Value)); }
            if (hostConfigSettings.ScanSkipPaths != null) { hostConfig.ScanSkipPaths.Clear (); hostConfig.ScanSkipPaths.AddRange (hostConfigSettings.ScanSkipPaths); }
            SetConfig (hostConfig);
        }
    }

    public class AppSettingsService : Service {
        public IAppSettings AppSettings { get; set; }
        public HostConfigSettingsExtended Config { get; set; }
        public object Any (AppSettingsRequest request) => this.AppSettings.GetAll ();
        public object Any (HostConfigRequest request) => this.Config;
    }

    [Route ("/settings")]
    public class AppSettingsRequest: IReturn<Dictionary<string, string>> { }

    [Route ("/config")]
    public class HostConfigRequest: IReturn<HostConfigSettingsExtended> { }

    public class HostConfigSettings {
        public bool AllowSessionCookies { get; set; }
        public bool AllowSessionIdsInHttpParams { get; set; }
        public bool OnlySendSessionCookiesSecurely { get; set; }
        public bool UseSaltedHash { get; set; }
        public string RestrictAllCookiesToDomain { get; set; }
        public bool LogUnobservedTaskExceptions { get; set; }
        public bool DisposeDependenciesAfterUse { get; set; }
        public bool WriteErrorsToResponse { get; set; }
        public bool ReturnsInnerException { get; set; }
        public TimeSpan DefaultJsonpCacheExpiration { get; set; }
        public bool AllowJsConfig { get; set; }
        public string AdminAuthSecret { get; set; }
        public bool DisableChunkedEncoding { get; set; }
        public bool EnableOptimizations { get; set; }
        public bool UseCamelCase { get; set; }
        public bool UseHttpsLinks { get; set; }
        public bool RedirectDirectoriesToTrailingSlashes { get; set; }
        public bool SkipFormDataInCreatingRequest { get; set; }
        public bool StripApplicationVirtualPath { get; set; }
        public bool RedirectToDefaultDocuments { get; set; }
        public bool AddRedirectParamsToQueryString { get; set; }
        public bool AllowAclUrlReservation { get; set; }
        public bool AllowNonHttpOnlyCookies { get; set; }
        public bool AllowPartialResponses { get; set; }
        public bool Return204NoContentForEmptyResponse { get; set; }
        public bool UseBclJsonSerializers { get; set; }
        public string DebugHttpListenerHostEnvironment { get; set; }
        public string DebugAspNetHostEnvironment { get; set; }
        public bool? StrictMode { get; set; }
        public bool DebugMode { get; set; }
        public bool AllowRouteContentTypeExtensions { get; set; }
        public bool AllowJsonpRequests { get; set; }
        public string ApiVersion { get; set; }
        public string WsdlServiceNamespace { get; set; }
        public string DefaultContentType { get; set; }
        public bool IgnoreWarningsOnAllProperties { get; set; }
        public bool EnableAccessRestrictions { get; set; }
        public string SoapServiceName { get; set; }
        public string MetadataRedirectPath { get; set; }
        public string DefaultRedirectPath { get; set; }
        public string WebHostPhysicalPath { get; set; }
        public string WebHostUrl { get; set; }
        public long? CompressFilesLargerThanBytes { get; set; }
        public string HandlerFactoryPath { get; set; }        
        public List<string> PreferredContentTypes { get; set; }
        public Feature EnableFeatures { get; set; }
        public Dictionary<string, TimeSpan> AddMaxAgeForStaticMimeTypes { get; set; }
        public Dictionary<string, string> GlobalResponseHeaders { get; set; }
        public List<string> AllowFilePaths { get; set; }
        public List<string> ForbiddenPaths { get; set; }
        public Dictionary<string, string> HtmlReplaceTokens { get; set; }
        public List<string> EmbeddedResourceTreatAsFiles { get; set; } /*HashSet<string>*/
        public List<string> CompressFilesWithExtensions { get; set; } /*HashSet<string>*/
        public List<string> AllowFileExtensions { get; set; } /*HashSet<string>*/
        public List<string> IgnoreFormatsInMetadata { get; set; } /*HashSet<string>*/
        public List<string> AppendUtf8CharsetOnContentTypes { get; set; } /*HashSet<string>*/
    }

    public class HostConfigSettingsExtended : HostConfigSettings {
        // the following properties cannot be set in the ServiceStack Api, so they 
        // have to be manipulated directly (see above in the Configure method of the AppHost)
        public List<string> DefaultDocuments { get; set; }
        public List<string> IgnoreWarningsOnPropertyNames { get; set; }        
        public List<string> RazorNamespaces { get; set; } /*HashSet<string>*/
        public Dictionary<string, string> RedirectPaths { get; set; }
        public List<string> ScanSkipPaths { get; set; }
    }
}