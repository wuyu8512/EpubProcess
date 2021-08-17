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
        private static readonly IConfiguration Config =
            Configuration.Default.WithCss().WithJs().WithConsoleLogger(c => new MyLog());

        private readonly HtmlParser _htmlParser;
        public override string[] Extension => new[] {".js"};

        public JavaScriptProcess()
        {
            var browsingContext = BrowsingContext.New(Config);
            _htmlParser = new HtmlParser(new HtmlParserOptions(), browsingContext);
        }

        public override async Task<int> ExecuteAsync(string script, EpubBook epub)
        {
            foreach (var id in epub.GetTextIDs())
            {
                var stream = epub.GetItemStreamByID(id);
                using var streamReader = new StreamReader(stream);
                var content = await streamReader.ReadToEndAsync();

                var document = await _htmlParser.ParseDocumentAsync(content);
                document.ExecuteScript(script);

                await using var streamWrite = new StreamWriter(stream);
                streamWrite.BaseStream.SetLength(0);
                document.ToHtml(streamWrite, new XhtmlMarkupFormatter());
            }

            return 0;
        }
    }
}