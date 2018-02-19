using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ServiceStack;

namespace demo
{
    public class CodeGenerator
    {
        public CodeGenerator()
        {
        }

        internal static Assembly Generate(List<Model> models, string cacheAssemblyInDir)
        {
            if (!Directory.Exists(cacheAssemblyInDir)) Directory.CreateDirectory(cacheAssemblyInDir);

            // see if the models have changed
            var assemblyFileName="models.dll";
            var assemblyFile = Path.Combine(cacheAssemblyInDir, assemblyFileName);
            var modelsJsonFile = Path.Combine(cacheAssemblyInDir, "models.json");
            var modelsJson = models.ToJson();
            if (File.Exists(assemblyFile) && File.Exists(modelsJsonFile))
            {
                if (File.ReadAllText(modelsJsonFile).Equals(modelsJson))
                    return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFile);
            }

            var modelCode = modelCodeFormat.Replace("{", "{{").Replace("}", "}}").Replace("{{{{", "{").Replace("}}}}", "}");
            var sb = new StringBuilder();
            foreach(var model in models)
            {
                sb.AppendLine(string.Format(modelCode, 
                    model.Singular, 
                    model.Plural, 
                    model.Slug.Trim('/'), 
                    string.Join("\n", model.Fields.Map(s => "public " + s.Type + " " + s.Name + " { get; set; }"))));
            }

            var nsCode = codeFormat.Replace("{", "{{").Replace("}", "}}").Replace("{{{{", "{").Replace("}}}}", "}");
            var code = string.Format(nsCode, sb.ToString(), typeof(CodeGenerator).Namespace);
            var tree = SyntaxFactory.ParseSyntaxTree(code);

            var requiredAssemblies = new List<string>
            {
                Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location,
                typeof(object).GetTypeInfo().Assembly.Location,
                typeof(Attribute).GetTypeInfo().Assembly.Location,
                typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).GetTypeInfo().Assembly.Location,
                typeof(System.Linq.Expressions.Expression).GetTypeInfo().Assembly.Location,
                typeof(ServiceStack.AppHostBase).GetTypeInfo().Assembly.Location,
                typeof(ServiceStack.RouteAttribute).GetTypeInfo().Assembly.Location,
                typeof(ServiceStack.Data.DbConnectionFactory).GetTypeInfo().Assembly.Location,
                typeof(ServiceStack.Configuration.IResolver).GetTypeInfo().Assembly.Location,
                typeof(ServiceStack.OrmLite.OrmLiteConnection).GetTypeInfo().Assembly.Location,
                typeof(CodeGenerator).GetTypeInfo().Assembly.Location,
            };

            var projectReferences = typeof(CodeGenerator).GetTypeInfo().Assembly.GetReferencedAssemblies();
            foreach(var ssr in projectReferences)
            {
                var asm = AssemblyLoadContext.Default.LoadFromAssemblyName(ssr);
                var asml = asm?.Location;
                if (asml != null && File.Exists(asml))
                {
                    requiredAssemblies.AddIfNotExists(asml);
                }
            }
            var requiredAssemblyReferences = requiredAssemblies.Map(r => MetadataReference.CreateFromFile(r));

            // A single, immutable invocation to the compiler to produce a library
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(assemblyFileName)
                .WithOptions(options)
                .AddReferences(requiredAssemblyReferences)
                .AddSyntaxTrees(tree);

            if (File.Exists(assemblyFile)) File.Delete(assemblyFile);
            var compilationResult = compilation.Emit(assemblyFile);
            if(compilationResult.Success)
            {
                // Cache the models json
                File.WriteAllText(modelsJsonFile, modelsJson);
                // Load the assembly
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFile);
            }
            else
            {
                foreach (Diagnostic codeIssue in compilationResult.Diagnostics)
                {
                    string issue = $@"ID: {codeIssue.Id}, Message: {codeIssue.GetMessage()},
                        Location: {codeIssue.Location.GetLineSpan()},
                        Severity: {codeIssue.Severity}";
                    Console.WriteLine(issue);
                }
                return null;
            }

            // OR IN MEMORY
            // compilation = CSharpCompilation.Create(Guid.NewGuid().ToString("N"))
            //     .WithOptions(options)
            //     .AddReferences(requiredAssemblyReferences)
            //     .AddSyntaxTrees(tree);
            // using (var ms = new MemoryStream())
            // {
            //     var compilationResult2 = compilation.Emit(ms);
            //     if (compilationResult2.Success)
            //     {
            //         ms.Position = 0;
            //         return AssemblyLoadContext.Default.LoadFromStream(ms);
            //     }
            //     else
            //     {
            //         foreach (Diagnostic codeIssue in compilationResult2.Diagnostics)
            //         {
            //             string issue = $@"ID: {codeIssue.Id}, Message: {codeIssue.GetMessage()},
            //                 Location: {codeIssue.Location.GetLineSpan()},
            //                 Severity: {codeIssue.Severity}";
            //             Console.WriteLine(issue);
            //         }
            //         return null;
            //     }
            // }
        }

        const string codeFormat = @"
            using System;
            using ServiceStack;
            using ServiceStack.Data;
            using ServiceStack.DataAnnotations;
            using ServiceStack.OrmLite;
            using {{1}};

            namespace Models
            {
                {{0}}
            }
            ";

            const string modelCodeFormat = @"
                public class {{0}}: IEntity
                {
                    [PrimaryKey, AutoIncrement]
                    public int Id { get; set; }
                    {{3}}
                }

                [Route(""/{{2}}"", ""GET"")]
                public class {{1}}Request
                {
                }

                [Route(""/{{2}}/{Id}"", ""GET"")]
                public class {{0}}GetRequest
                {
                    public int Id { get; set; }
                }

                [Route(""/{{2}}"", ""POST"")]
                public class {{0}}PostRequest: {{0}}
                {
                }

                [Route(""/{{2}}/{Id}"", ""PUT"")]
                public class {{0}}PutRequest: {{0}}
                {
                }

                [Route(""/{{2}}/{Id}"", ""Delete"")]
                public class {{0}}DeleteRequest: {{0}}
                {
                }
                
                public class {{0}}Service: Service
                {
                    public object Get({{1}}Request request)
                    {
                        return Db.Select<{{0}}>();
                    }
                    public object Get({{0}}GetRequest request)
                    {
                        return Db.SingleById<{{0}}>(request.Id);
                    }
                    public object Post({{0}}PostRequest request)
                    {
                        var id = Db.Insert<{{0}}>(({{0}}) request, true);
                        return Db.SingleById<{{0}}>(request.Id);
                    }
                    public object Put({{0}}PutRequest request)
                    {
                        Db.Update<{{0}}>(({{0}}) request);
                        return Db.SingleById<{{0}}>(request.Id);
                    }
                    public object Delete({{0}}DeleteRequest request)
                    {
                        return Db.Delete<{{0}}>(r => r.Id == request.Id);
                    }
                }
                ";
    }
}
