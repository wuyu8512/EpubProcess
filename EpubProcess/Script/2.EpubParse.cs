﻿using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Xhtml;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuyu.Epub;
using System;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Css.Dom;
using System.Text.RegularExpressions;

namespace EpubProcess
{
    class EpubParse : Script
    {
        private static readonly IConfiguration Config = Configuration.Default.WithCss().WithJs();
        private static readonly IBrowsingContext Context = BrowsingContext.New(Config);
        private static readonly HtmlParser HtmlParser = new(new HtmlParserOptions(), Context);

        public override async Task<int> ParseAsync(EpubBook epub)
        {
            // 删掉所有的Style文件，用默认的替换
            epub.GetItemIDs(new[] { ".css" }).ToArray().ForEach(item => epub.DeleteItem(item));

            epub.AddItem(new EpubItem
            {
                EntryName = "Styles/style.css",
                ID = "style.css",
                Data = await File.ReadAllBytesAsync("./Script/Res/style.css")
            });

            // 添加注释用图片
            epub.AddItem(new EpubItem
            {
                EntryName = "Images/note.png",
                ID = "note.png",
                Data = await File.ReadAllBytesAsync("./Script/Res/note.png")
            });

            // 进行内文代码处理
            foreach (var id in epub.GetTextIDs())
            {
                var stream = epub.GetItemStreamByID(id);
                using var streamReader = new StreamReader(stream, Encoding.UTF8);
                var content = await streamReader.ReadToEndAsync();

                var doc = await HtmlParser.ParseDocumentAsync(content);

                ProcessImage(doc);
                RemoveEmptyParagraphElement(doc);
                ProcessClass(doc);

                await using var streamWrite = new StreamWriter(stream, Encoding.UTF8);
                streamWrite.BaseStream.SetLength(0);
                doc.ToHtml(streamWrite, XhtmlMarkupFormatter.Instance);
            }

            await ProcessCover(epub); // 必须在图片处理完之后运行
            ProcessNav(epub);

            return 0;
        }

        // 删除章节开头没用的空行
        private void RemoveEmptyParagraphElement(IHtmlDocument doc)
        {
            if (doc.Body.FirstElementChild.ChildElementCount >= 4)
            {
                var main = doc.Body.FirstElementChild;

                var node = main.FirstElementChild;
                while (node is IHtmlParagraphElement && node.IsEmpty())
                {
                    var next = node.NextElementSibling;
                    node.Remove();
                    node = next;
                }
            }
        }

        // 图片处理
        private void ProcessImage(IHtmlDocument doc)
        {
            // 处理彩页图片
            foreach (var svg in doc.QuerySelectorAll("svg"))
            {
                var svgImg = svg.QuerySelector("image");
                var src = svgImg.GetAttribute("href");

                var div = doc.CreateElement("div");
                div.ClassName = "illus duokan-image-single";

                var img = doc.CreateElement("img");
                img.SetAttribute("alt", Path.GetFileNameWithoutExtension(src));
                img.SetAttribute("src", src);
                div.AppendChild(img);

                svg.OuterHtml = div.ToXhtml();
            }
            // 处理黑白图片
            foreach (var imgNode in doc.QuerySelectorAll("p img"))
            {
                if (imgNode.ParentElement is IHtmlParagraphElement && imgNode.ParentElement.ChildElementCount == 1 && imgNode.ParentElement.TextContent.IsEmpty())
                {
                    var src = imgNode.GetAttribute("src");

                    var div = doc.CreateElement("div");
                    div.ClassName = "illus duokan-image-single";

                    var img = doc.CreateElement("img");
                    img.SetAttribute("alt", Path.GetFileNameWithoutExtension(src));
                    img.SetAttribute("src", src);
                    div.AppendChild(img);

                    imgNode.ParentElement.OuterHtml = div.ToXhtml();
                }
            }
        }

        private async Task ProcessCover(EpubBook epub)
        {
            var html = epub.GetCoverXhtml();
            if (html == null && epub.Cover.IsEmpty())
            {
                Console.WriteLine("本epub无法找到封面图片，跳过封面处理");
                return;
            }
            var doc = await HtmlParser.ParseDocumentAsync(epub.GetItemContentByID(epub.GetItemByHref(html.Href).ID));
            var img = (IHtmlImageElement)doc.QuerySelector("img");
            var src = img.GetAttribute("src");
            epub.CreateCoverXhtml(epub.GetItemByHref(Util.ZipResolvePath(Path.GetDirectoryName(html.Href), src)).ID);
        }

        // 处理特殊样式
        private void ProcessClass(IHtmlDocument doc)
        {
            doc.QuerySelectorAll("span.tcy").ForEach(span => span.OuterHtml = span.TextContent.Trim());
        }

        private void ProcessNav(EpubBook epub)
        {
            var nav = epub.GetNav();
            if (nav == null)
            {
                Console.WriteLine("本epub无法找到目录，跳过目录处理");
                return;
            }
            var last = epub.Nav.Last();
            if (last.Title == "版權頁")
            {
                // TODO 目录
                var id = epub.GetItemByHref(last.Href.Split('#')[0]).ID;
                epub.DeleteItem(id);
                last.Remove();
            }
        }
    }
}
