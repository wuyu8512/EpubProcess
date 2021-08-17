using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuyu.Epub;
using AngleSharp.Html.Parser;
using AngleSharp;

namespace EpubProcess
{
    class EpubFormat : Script
    {
        public override async Task<int> ParseAsync(EpubBook epub)
        {
            var Config = Configuration.Default.WithCss().WithJs();
            var browsingContext = BrowsingContext.New(Config);
            var _htmlParser = new HtmlParser(new HtmlParserOptions(), browsingContext);

            foreach (var id in epub.GetTextIDs())
            {
                var stream = epub.GetItemStreamByID(id);
                using var streamReader = new StreamReader(stream, Encoding.UTF8);
                var content = await streamReader.ReadToEndAsync();

                // 解析
                var doc = await _htmlParser.ParseDocumentAsync(content);

                using var sw = new StreamWriter(new MemoryStream());
                // 标准化
                var body = doc.Body.ChildNodes.ToXhtml();
                var xhtmlTemplate = await File.ReadAllTextAsync("./Script/Res/html.txt");
                var html = string.Format(xhtmlTemplate, doc.Title, body);

                await using var streamWrite = new StreamWriter(stream, Encoding.UTF8);
                streamWrite.BaseStream.SetLength(0);
                await streamWrite.WriteAsync(html);
            }
            return 0;
        }
    }
}
