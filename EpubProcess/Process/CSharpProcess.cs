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

namespace EpubProcess.Process
{
    class CSharpProcess : BaseProcess
    {
        private static readonly Lazy<IEnumerable<PortableExecutableReference>> References = new(
            () => AppDomain.CurrentDomain.GetAssemblies().Select(x => MetadataReference.CreateFromFile(x.Location))
        );

        private static readonly CSharpParseOptions Options =
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

        public override string[] Extension { get; } = { ".cs" };

        public override async Task<int> ExecuteAsync(IEnumerable<string> scripts, EpubBook epub)
        {
            foreach (var script in scripts)
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
                var result = compilation.Emit(ms);

                if (result.Success)
                {
                    var assembly = Assembly.Load(ms.ToArray());
                    var type = assembly.GetTypes().First();
                    var instance = (Script)Activator.CreateInstance(type);
                    Debug.Assert(instance != null, nameof(instance) + " != null");
                    await instance.ParseAsync(epub);
                }
                else
                {
                    throw new BuildException(result.Diagnostics);
                }
            }
            return 0;
        }
    }
}