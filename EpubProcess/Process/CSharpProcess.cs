using EpubProcess.Process;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Wuyu.Epub;
using System.Text.Json;

namespace EpubProcess.Process
{
    class CSharpProcess : BaseProcess
    {
        private static readonly Lazy<IEnumerable<PortableExecutableReference>> References = new(
            () =>
            {
                var references = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location)).Select(x => 
                {
                    return MetadataReference.CreateFromFile(x.Location);
                }).ToList();

                foreach (var file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll"))
                {
                    references.Add(MetadataReference.CreateFromFile(file));
                }

                return references;
            }
        );

        private static readonly CSharpParseOptions Options =
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

        public override string[] Extension { get; } = { ".cs" };

        public override async Task<int> ExecuteAsync(string script, EpubBook epub)
        {
            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(script, Options);
            var compilation = CSharpCompilation.Create($"{script.GetHashCode()}.dll",
                    new[] { parsedSyntaxTree },
                    //references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                        optimizationLevel: OptimizationLevel.Release,
                        assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default))
                .AddReferences(References.Value);
            await using var ms = new MemoryStream();
            await using var msPdb = new MemoryStream();
            var result = compilation.Emit(ms, msPdb);

            if (result.Success)
            {
                var assembly = Assembly.Load(ms.ToArray(), msPdb.ToArray());
                var type = assembly.GetTypes().First();
                var instance = (Script)Activator.CreateInstance(type);
                Debug.Assert(instance != null, nameof(instance) + " != null");
                await instance.ParseAsync(epub);
            }
            else
            {
                throw new BuildException(result.Diagnostics);
            }

            return 0;
        }
    }
}