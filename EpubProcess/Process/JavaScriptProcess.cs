using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Js;
using AngleSharp.Xhtml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Wuyu.Epub;

namespace EpubProcess.Process
{
    class JavaScriptProcess : BaseProcess
    {
        private readonly static IConfiguration config = Configuration.Default.WithCss().WithXml().WithJs().WithConsoleLogger(c => new MyLog());
        private readonly IBrowsingContext browsingContext;
        private readonly HtmlParser htmlParser;
        public override string[] Extension => new[] { ".js" };

        public JavaScriptProcess()
        {
            browsingContext = BrowsingContext.New(config);
            htmlParser = new HtmlParser(new HtmlParserOptions(), browsingContext);
        }

        public override async Task<int> ExecuteAsync(IEnumerable<string> scripts, EpubBook epub)
        {
            foreach (var id in epub.GetTextIDs())
            {
                var stream = epub.GetItemByID(id);
                using var streamReader = new StreamReader(stream);
                var content = await streamReader.ReadToEndAsync();

                var document = await htmlParser.ParseDocumentAsync(content);
                foreach (var script in scripts)
                {
                    document.ExecuteScript(script);
                }

                content = document.ToHtml(new XhtmlMarkupFormatter());

                await using var streamWrite = new StreamWriter(stream);
                streamWrite.BaseStream.SetLength(0);
                await streamWrite.WriteAsync(content);
            }

            return 0;
        }
    }
}
