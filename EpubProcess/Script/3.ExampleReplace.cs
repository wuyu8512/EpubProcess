using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Wuyu.Epub;
using System;

namespace EpubProcess
{
    /// <summary>
    /// 执行简单的替换
    /// </summary>
    public class ExampleReplace : Script
    {
        private static readonly IConfiguration Config = Configuration.Default.WithCss().WithJs();
        private static readonly IBrowsingContext Context = BrowsingContext.New(Config);
        private static readonly HtmlParser HtmlParser = new(new HtmlParserOptions(), Context);

        private static readonly Dictionary<char, char> ReplaceCharDir = new[]
        {
            ('妳','你'),
            ('姊','姐'),
        }.ToDictionary(x => x.Item1, x => x.Item2);

        public override async Task<int> ParseAsync(EpubBook epub)
        {
            foreach (var id in epub.GetTextIDs())
            {
                var stream = epub.GetItemStreamByID(id);
                using var streamReader = new StreamReader(stream);
                var content = await streamReader.ReadToEndAsync();

                // 普通替换
                content = content.Replace("align-end", "right");

                // 正则替换
                //content = Regex.Replace(content, "妳", "你");

                // 单字符替换
                var doc = await HtmlParser.ParseDocumentAsync(content);
                ReplaceChar(doc);

                await using var streamWrite = new StreamWriter(stream);
                streamWrite.BaseStream.SetLength(0);
                await streamWrite.WriteAsync(content);
            }
            return 0;
        }

        private static void ReplaceChar(IDocument doc)
        {
            var nodeIterator = doc.CreateNodeIterator(doc.Body, FilterSettings.Text);
            INode currentNode;
            while ((currentNode = nodeIterator.Next()) != null)
            {
                var str = currentNode.TextContent.ToCharArray();
                str = str.Select(ch => ReplaceCharDir.ContainsKey(ch) ? ReplaceCharDir[ch] : ch).ToArray();
                currentNode.TextContent = new string(str);
            }
        }
    }
}
