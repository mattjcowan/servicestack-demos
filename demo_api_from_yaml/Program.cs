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
using ServiceStack.Api.OpenApi;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;

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

            this.RootDir = services.GetRequiredService<IHostingEnvironment>().ContentRootPath;
            this.DataDir = Path.Combine(this.RootDir, "App_Data");
            Directory.CreateDirectory(this.DataDir);            
        }

        public string RootDir { get; set; }
        public string DataDir { get; set; }

        public override void Configure(Container container)
        {
            var dbFactory = new OrmLiteConnectionFactory(Path.Combine(this.DataDir, "db.sqlite"), SqliteDialect.Provider);
            this.Register<IDbConnectionFactory>(dbFactory);

            var yamlSrcFile = Path.Combine(this.RootDir, "models.yaml");
            if (File.Exists(yamlSrcFile))
            {
                var models = File.ReadAllText(yamlSrcFile).FromYaml<List<Model>>();
                var asm = CodeGenerator.Generate(models, this.DataDir);
                if (asm != null) 
                {
                    using (var db = dbFactory.OpenDbConnection())
                    {
                        foreach(var entityType in asm.GetExportedTypes().Where(t => 
                            t.HasInterface("IEntity", true, false) &&
                            t.IsClass && !t.IsAbstract))
                        {
                            db.CreateTableIfNotExists(entityType);
                        };
                    }

                    this.AddPluginsFromAssembly(asm);
                    this.RegisterServicesInAssembly(asm);
                } 
                else
                {
                    Console.WriteLine("NO assembly generated");
                }
            }

            Plugins.Add(new OpenApiFeature { UseCamelCaseSchemaPropertyNames = true });
        }
    }

    public static class TypeExtensions 
    {
        public static IEnumerable<Type> GetInterfaces(this Type type, bool includeInherited)
        {
            if (includeInherited || type.BaseType == null)
                return type.GetInterfaces();
            else
                return type.GetInterfaces().Except(type.BaseType.GetInterfaces());
        }

        public static bool HasInterface(this Type type, string name, bool ignoreCase, bool includeInherited)
        {
            if (includeInherited)
                return type.GetInterface(name, ignoreCase) != null;
            
            return type.GetInterfaces(includeInherited)
                .Any(i => 
                    i.Name.Equals(name, ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal));
        }
    }

    public static class SerializationExtensions
    {
        public static T FromYaml<T>(this string target)
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(new YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention())
                .Build();
            return deserializer.Deserialize<T>(target);
        }

        public static string ToYaml<T>(this T target)
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder()
                .WithNamingConvention(new YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention())
                .Build();
            return serializer.Serialize(target);
        }
    }

    public interface IEntity
    {
    }

    public class Model
    {
        public Model() {}
        public Model(string singular, string plural, string slug, params Field[] fields)
        {
            Singular = singular;
            Plural = plural;
            Slug = slug;
            Fields = fields;
        }

        public string Singular { get; set; } 
        public string Plural { get; set; } 
        public string Slug { get; set; } 
        public string Tag { get; set; } 
        public Field[] Fields { get; set; }
        public string Description { get; set; }
        public CRUD Roles { get; set; }
    }

    public class Field<FType>: Field
    {
        public Field() {}
        public Field(string name, bool nullable = false): base(typeof(FType).FullName, name, nullable) {}
    }

    public class Field
    {
        public Field() {}
        public Field(string type, string name, bool nullable = false) 
        {
            Type = type + (nullable ? "?": "");
            Name = name;
        }

        public string Type { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public CRUD Roles { get; set; }
    }

    public class CRUD
    {
        public string Create { get; set; }
        public string Read { get; set; }
        public string Update { get; set; }
        public string Delete { get; set; }
    }

    public class Reference
    {
        public string Type { get; set; }
        public string Cardinality { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
    }
}
