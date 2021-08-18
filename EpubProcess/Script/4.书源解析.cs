using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Xhtml;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuyu.Epub;
using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace EpubProcess
{
    class EpubParse : Script
    {
        private static readonly IConfiguration Config = Configuration.Default.WithCss().WithJs();
        private static readonly IBrowsingContext Context = BrowsingContext.New(Config);
        private static readonly HtmlParser HtmlParser = new(new HtmlParserOptions(), Context);

        private static readonly char[] EmptyChar = new[] { ' ', '\r', '\n', '\t', '　' };

        public override async Task<int> ParseAsync(EpubBook epub)
        {
            // 删掉所有的Style文件，用默认的替换，此处必须ToArray，否则会遇到多次迭代问题
            epub.GetItems(new[] { ".css" }).ToArray().ForEach(item => epub.DeleteItem(item.ID));

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
                using var streamReader = new StreamReader(stream);
                var content = await streamReader.ReadToEndAsync();

                var doc = await HtmlParser.ParseDocumentAsync(content);

                ProcessImage(doc);
                RemoveEmptyParagraphElement(doc);
                ProcessClass(doc);

                await using var streamWrite = new StreamWriter(stream);
                streamWrite.BaseStream.SetLength(0);
                doc.ToHtml(streamWrite, XhtmlMarkupFormatter.Instance);
            }

            await ProcessCover(epub); // 必须在图片处理完之后运行
            await ProcessNav(epub);
            await ProcessTitle(epub);

            return 0;
        }

        // 删除章节头尾没用的空行
        private static void RemoveEmptyParagraphElement(IHtmlDocument doc)
        {
            if (doc.Body.FirstElementChild.ChildElementCount >= 3)
            {
                var main = doc.Body.FirstElementChild;
                RemoveEmptyParagraphElement(main.FirstElementChild);
                RemoveEmptyParagraphElementReverse(main.LastElementChild);
            }
        }

        private static void RemoveEmptyParagraphElement(IElement element)
        {
            while (!(element is IHtmlImageElement) && element.IsEmpty())
            {
                var next = element.NextElementSibling;
                element.Remove();
                element = next;
            }
        }

        private static void RemoveEmptyParagraphElementReverse(IElement element)
        {
            while (!(element is IHtmlImageElement) && element.IsEmpty())
            {
                var next = element.PreviousElementSibling;
                element.Remove();
                element = next;
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
            string id;
            if (epub.Cover.IsEmpty())
            {
                var doc = await HtmlParser.ParseDocumentAsync(epub.GetItemContentByID(epub.GetItemByHref(html.Href).ID));
                var img = (IHtmlImageElement)doc.QuerySelector("img");
                var src = img.GetAttribute("src");
                id = epub.GetItemByHref(Util.ZipResolvePath(Path.GetDirectoryName(html.Href), src)).ID;
            }
            else
            {
                id = epub.Cover;
            }

            epub.CreateCoverXhtml(id);
        }

        // 处理特殊样式
        private void ProcessClass(IHtmlDocument doc)
        {
            doc.QuerySelectorAll("span.tcy").ForEach(span => span.OuterHtml = span.TextContent.Trim(EmptyChar));
            doc.QuerySelectorAll("span.sideways").ForEach(span => span.OuterHtml = span.TextContent.Trim(EmptyChar));
        }

        private async Task ProcessNav(EpubBook epub)
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
                var basePath = Path.GetDirectoryName(nav.Href);
                var id = epub.GetItemByHref(Util.ZipResolvePath(basePath, last.Href.Split('#')[0])).ID;
                var content = epub.GetItemContentByID(id);
                var doc = await HtmlParser.ParseDocumentAsync(content);
                foreach (IHtmlImageElement img in doc.QuerySelectorAll("img"))
                {
                    var src = img.GetAttribute("src");
                    var imgPath = Util.ZipResolvePath(basePath, src);
                    var imgItem = epub.GetItemByHref(imgPath);
                    epub.DeleteItem(imgItem.ID);
                }

                epub.DeleteItem(id);
                last.Remove();
            }
        }

        private async Task ProcessTitle(EpubBook epub)
        {
            var nav = epub.GetNav();
            if (nav == null)
            {
                Console.WriteLine("本epub无法找到目录，跳过标题处理");
                return;
            }

            var basePath = Path.GetDirectoryName(nav.Href);
            foreach (var item in epub.Nav)
            {
                var href = Util.ZipResolvePath(basePath, item.Href.Split('#')[0]);
                var id = epub.GetItemByHref(href).ID;
                var content = await epub.GetItemContentByIDAsync(id);

                var doc = await HtmlParser.ParseDocumentAsync(content);
                doc.Title = item.Title;

                foreach (var pNode in doc.QuerySelectorAll("p").Take(10))
                {
                    if (pNode.TextContent.Trim(EmptyChar) == item.Title)
                    {
                        RemoveEmptyParagraphElement(pNode.NextElementSibling);
                        if (pNode.OuterHtml.IndexOf(item.Title) > -1) pNode.OuterHtml = $"<h4>{item.Title}</h4>";
                        else pNode.OuterHtml = $"<h4>{pNode.InnerHtml}</h4>";
                        break;
                    }
                }

                await epub.SetItemContentByIDAsync(id, doc.ToXhtml());
            }
        }
    }
}
