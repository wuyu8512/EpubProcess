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
            () =>
            {
                var path = AppDomain.CurrentDomain.GetAssemblies().First(x => Path.GetFileName(x.Location) == "System.Private.CoreLib.dll").Location;

                var fileList = new HashSet<string>();
                AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location)).ForEach(x => fileList.Add(x.Location));
                Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll").ForEach(x => fileList.Add(x));
                Directory.GetFiles(Path.GetDirectoryName(path), "System.*.dll").ForEach(x => fileList.Add(x));

                fileList = fileList.Where(x => !x.Contains("Native")).ToHashSet();

                return fileList.Select(x => MetadataReference.CreateFromFile(x));
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
                var types = assembly.GetTypes();
                var type = types.FirstOrDefault(x => x.BaseType == typeof(Script));
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