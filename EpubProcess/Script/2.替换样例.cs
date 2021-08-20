using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Wuyu.Epub;

namespace EpubProcess
{
    /// <summary>
    /// 执行简单的替换，本文件为通用模板，可照抄代码改成针对不同书的专用模板
    /// </summary>
    public class ExampleReplace : Script
    {
        private static readonly IConfiguration Config = Configuration.Default.WithCss().WithJs();
        private static readonly IBrowsingContext Context = BrowsingContext.New(Config);
        private static readonly HtmlParser HtmlParser = new(new HtmlParserOptions(), Context);

        private static readonly Dictionary<char, char> ReplaceCharDir = new[]
        {
            //('　',' '),
            ('~','～'),
            ('─','—'),
            ('╳','×'),
            ('‥','：'),
            ('︽','《'),('︾','》'),
            ('﹁','「'),('﹂','」'),
            ('﹃','『'),('﹄','』'),
            ('︿','〈'),('﹀','〉'),
        }.ToDictionary(x => x.Item1, x => x.Item2);

        private static readonly (string key, string value)[] ReplaceString = new[]
        {
            ("align-end","right"),
            ("align-center","center"),
            ("書封","封面"),
            ("em-dot","dot"),
            ("――","——"),
            ("後　記","後記"),
            ("目　錄","目錄"),
        };

        private static readonly (string key, string value)[] ReplaceReg = new[]
        {
            ("<p>[　 ]+</p>","<p><br/></p>"),
            ("<p>[　 ]+","<p>"),
            ("[　 ]+</p>","</p>"),
            ("──|－－|—— | ——|――","——"),
            (@"\.\.\.\.\.\.|⋯⋯","……"),
            ("~|∼|〜","～"),
            ("•|‧|・|．|˙|･|·","•"),
        };

        public override async Task<int> ParseAsync(EpubBook epub)
        {
            foreach (var item in epub.GetHtmlItems())
            {
                Console.WriteLine(item.ID);
                using var stream = epub.GetItemStreamByID(item.ID);
                using var streamReader = new StreamReader(stream);
                var content = await streamReader.ReadToEndAsync();

                // 普通替换
                ReplaceString.ForEach((item) => content = content.Replace(item.key, item.value));

                // 正则替换
                ReplaceReg.ForEach((item) => content = Regex.Replace(content, item.key, item.value));

                // 单字符替换
                var doc = await HtmlParser.ParseDocumentAsync(content);
                ReplaceChar(doc);

                await using (var streamWrite = new StreamWriter(stream))
                {
                    streamWrite.BaseStream.SetLength(0);
                    await streamWrite.WriteAsync(doc.ToXhtml());
                }

                if (item.IsNav) epub.UpDataNav();
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
