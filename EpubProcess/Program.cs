﻿using AngleSharp;
using AngleSharp.Html.Parser;
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
        //public static readonly IConfiguration Config = Configuration.Default.WithCss().WithJs();
        //public static readonly IBrowsingContext Context = BrowsingContext.New(Config);
        //public static readonly HtmlParser HtmlParser = new(new HtmlParserOptions(), Context);

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = new System.Text.UTF8Encoding(false);

            var epubPath = args[0];
            var outPath = epubPath.Replace(Path.GetExtension(epubPath), string.Empty) + "_Process.epub";
            var epub = EpubBook.ReadEpub(new FileStream(epubPath, FileMode.Open),
                new FileStream(outPath, FileMode.Create));
            var watch = new Stopwatch();

            //EpubParse epubParse = new();
            //await epubParse.ParseAsync(epub);
            //await epubParse.ParseAsync(epub);

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
                    Console.WriteLine("脚本运行完毕，用时{0}毫秒", watch.ElapsedMilliseconds);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("-----------------------------------------------------");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                catch (BuildException e)
                {
                    Console.WriteLine(e.ToString());
                    foreach (var diagnostic in e.Diagnostics)
                    {
                        Console.WriteLine(diagnostic.ToString());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            epub.Dispose();
        }
    }
}