using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuyu.Epub;
using AngleSharp.Html.Parser;

namespace EpubProcess
{
    class EpubFormat : Script
    {
        private static readonly HtmlParser Parser = new HtmlParser();

        public override async Task<int> ParseAsync(EpubBook epub)
        {
            foreach (var id in epub.GetTextIDs())
            {
                var stream = epub.GetItemStreamByID(id);
                using var streamReader = new StreamReader(stream, Encoding.UTF8);
                var content = await streamReader.ReadToEndAsync();

                var doc = await Parser.ParseDocumentAsync(content);
                var dodyHtml = doc.Body.InnerHtml;

                var xhtmlTemplate = await File.ReadAllTextAsync("./Script/Res/html.txt");
                var html = string.Format(xhtmlTemplate, doc.Title, dodyHtml);

                await using var streamWrite = new StreamWriter(stream, Encoding.UTF8);
                streamWrite.BaseStream.SetLength(0);
                await streamWrite.WriteAsync(html);
            }
            return 0;
        }
    }
}
