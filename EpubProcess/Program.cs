using EpubProcess.Process;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Wuyu.Epub;

namespace EpubProcess
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            var epubPath = args[0];
            var outPath = epubPath.Replace(Path.GetExtension(epubPath), string.Empty) + "_out.epub";
            var epub = EpubBook.ReadEpub(new FileStream(epubPath, FileMode.Open),
                new FileStream(outPath, FileMode.Create));

            var extensionGroup = Directory.GetFiles("./Script").GroupBy(c => Path.GetExtension(c).ToLower());
            foreach (var group in extensionGroup)
            {
                var extension = group.Key;
                var process = BaseProcess.Processes.FirstOrDefault(c => c.Extension.Contains(extension, StringComparer.CurrentCultureIgnoreCase));
                if (process == default) continue;
                try
                {
                    await process.ExecuteAsync(group.Select((c) => File.ReadAllText(c)), epub);
                }
                catch (BuildException e)
                {
                    Console.WriteLine(e.Message);
                    foreach (var diagnostic in e.Diagnostics)
                    {
                        Console.WriteLine(diagnostic.ToString());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            epub.Dispose();
        }
    }
}