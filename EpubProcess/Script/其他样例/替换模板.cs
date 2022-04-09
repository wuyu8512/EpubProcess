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
using System.Xml.Linq;

namespace EpubProcess
{
    /// <summary>
    /// 执行简单的替换，本文件为通用模板，可照抄代码改成针对不同书的专用模板
    /// </summary>
    public class 替换模板 : Script
    {
        private static readonly IConfiguration Config = Configuration.Default.WithCss().WithJs();
        private static readonly IBrowsingContext Context = BrowsingContext.New(Config);
        private static readonly HtmlParser HtmlParser = new(new HtmlParserOptions(), Context);

        // 普通替换，用于只有一个字符的
        private static readonly Dictionary<char, char> ReplaceCharDir = new[]
        {
            ('!','！'),
        }.ToDictionary(x => x.Item1, x => x.Item2);

        // 普通替换
        private static readonly (string key, string value)[] ReplaceString = new[]
        {
            ("我是被替换内容", "我是替换内容"),
            ("我是被替换内容", "我是替换内容"),
        };

        // 正则替换
        private static readonly (string key, string value)[] ReplaceReg = new[]
        {
            ("我是正则", "我是替换内容"),
            ("我是正则", "我是替换内容"),
        };

        public override async Task<int> ParseAsync(EpubBook epub)
        {
            foreach (var item in epub.GetHtmlItems())
            {
                using var stream = epub.GetItemStreamByID(item.ID);
                using var streamReader = new StreamReader(stream);
                var content = await streamReader.ReadToEndAsync();

                // 普通替换
                ReplaceString.ForEach((item) => content = content.Replace(item.key, item.value));

                // 正则替换
                ReplaceReg.ForEach((item) => content = Regex.Replace(content, item.key, item.value));

                // 单字符替换
                var doc = XDocument.Parse(content);
                ReplaceChar(doc);

                await using (var streamWrite = new StreamWriter(stream))
                {
                    streamWrite.BaseStream.SetLength(0);
                    await doc.SaveAsync(streamWrite, SaveOptions.None, default);
                }

                if (item.IsNav) epub.UpDataNav();
            }
            return 0;
        }

        private static void ReplaceChar(XContainer container)
        {
            foreach (var currentNode in container.Descendants().Where(x => !x.HasElements))
            {
                var str = currentNode.Value.ToCharArray();
                str = str.Select(ch => ReplaceCharDir.ContainsKey(ch) ? ReplaceCharDir[ch] : ch).ToArray();
                currentNode.Value = new string(str);
            }
        }
    }
}
