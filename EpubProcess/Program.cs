using EpubProcess.Process;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            var watch = new Stopwatch();

            var files = Directory.GetFiles($".{Path.DirectorySeparatorChar}Script").ToArray();
            Array.Sort(files);
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file);
                var process = BaseProcess.Processes.FirstOrDefault(c =>
                    c.Extension.Contains(extension, StringComparer.CurrentCultureIgnoreCase));
                if (process == default) continue;
                try
                {
                    Console.WriteLine("正在运行脚本：{0}", file);
                    watch.Restart();
                    await process.ExecuteAsync(await File.ReadAllTextAsync(file), epub);
                    watch.Stop();
                    Console.WriteLine("脚本运行完毕，用时{0}毫秒\r\n", watch.ElapsedMilliseconds);
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
                    Console.WriteLine(e.ToString());
                }
            }

            epub.Dispose();
        }
    }
}