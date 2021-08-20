using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuyu.Epub;
using AngleSharp.Html.Parser;
using AngleSharp;
using System.Xml.Linq;

namespace EpubProcess
{
    /// <summary>
    /// 负责格式化代码
    /// </summary>
    class EpubFormat : Script
    {
        public override async Task<int> ParseAsync(EpubBook epub)
        {
            var Config = Configuration.Default.WithCss().WithJs();
            var browsingContext = BrowsingContext.New(Config);
            var _htmlParser = new HtmlParser(new HtmlParserOptions(), browsingContext);

            var xhtmlTemplate = await File.ReadAllTextAsync("./Script/Res/content.html");

            foreach (var id in epub.GetTextIDs())
            {
                var stream = epub.GetItemStreamByID(id, out var href);
                using var streamReader = new StreamReader(stream);
                var content = await streamReader.ReadToEndAsync();

                // 解析
                var doc = await _htmlParser.ParseDocumentAsync(content);

                // 标准化
                var body = doc.Body.ChildNodes.ToXhtml();
                var html = string.Format(xhtmlTemplate, doc.Title, body);

                XDocument xDocument = XDocument.Parse(html);
                // TODO Style改进

                stream.SetLength(0);
                stream.Position = 0;
                await using var streamWrite = new StreamWriter(stream);
                await xDocument.SaveAsync(streamWrite, SaveOptions.None, System.Threading.CancellationToken.None);
            }

            var nav = epub.GetNav();
            if (nav != null)
            {
                var content = epub.Nav.BaseElement.Document.ToString();
                var doc = await _htmlParser.ParseDocumentAsync(content);
                var body = doc.Body.ChildNodes.ToXhtml();
                var html = string.Format(await File.ReadAllTextAsync("./Script/Res/nav.html"), doc.Title, body);

                XDocument xDocument = XDocument.Parse(html);
                await epub.SetItemContentByIDAsync(nav.ID, xDocument.ToString());
                epub.UpDataNav();
            }

            return 0;
        }
    }
}
