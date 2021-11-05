using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Wuyu.Epub;

namespace EpubProcess
{
    class EpubParse : Script
    {
        private static readonly IConfiguration Config = Configuration.Default.WithCss().WithJs();
        private static readonly IBrowsingContext Context = BrowsingContext.New(Config);
        private static readonly IHtmlParser HtmlParser = Context.GetService<IHtmlParser>();

        private static readonly char[] EmptyChar = new[] { ' ', '\r', '\n', '\t', '　' };

        private static string IllusAuthor = "{0}";

        public override async Task<int> ParseAsync(EpubBook epub)
        {
            // 删掉js和json，居然还有这两种文件????
            // 删掉所有的Style文件，用默认的替换，此处必须ToArray，否则会遇到多次迭代问题
            epub.GetItems(new[] { ".css", ".js", ".json" }).ToArray().ForEach(item => epub.DeleteItem(item.ID));
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

                var doc = HtmlParser.ParseDocument(content);

                ProcessImage(doc);
                RemoveEmptyParagraphElement(doc);
                ProcessClass(doc);

                stream.SetLength(0);
                await using var streamWrite = new StreamWriter(stream);
                await streamWrite.WriteAsync(doc.ToXhtml());
            }

            ProcessPackage(epub);
            await ProcessCover(epub); // 必须在图片处理完之后运行
            await ProcessNav(epub);
            await ProcessTitle(epub);
            await AddMessageAndIllus(epub);

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
        private static void ProcessImage(IHtmlDocument doc)
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
            foreach (var imgNode in doc.QuerySelectorAll("p img:first-child ,div img:first-child"))
            {
                // 无法很好的处理所有情况，需要后续观察
                if (imgNode.ParentElement.ClassName == "flower")
                {
                    imgNode.ParentElement.RemoveAttribute("class");
                    imgNode.RemoveAttribute("class");
                }
                else if ((imgNode.ParentElement is IHtmlParagraphElement || imgNode.ParentElement is IHtmlDivElement) && imgNode.ParentElement.TextContent.IsEmpty())
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

        // 尝试处理封面html文件
        private static async Task ProcessCover(EpubBook epub)
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
                if (img != null)
                {
                    var src = img.GetAttribute("src");
                    id = epub.GetItemByHref(Util.ZipResolvePath(Path.GetDirectoryName(html.Href), src)).ID;
                }
                else
                {
                    Console.WriteLine("本epub无法找到封面图片，跳过封面处理");
                    return;
                }
            }
            else
            {
                id = epub.Cover;
            }

            epub.CreateCoverXhtml(id);

            var coverNav = epub.Nav.FirstOrDefault(x => x.Title == "封面");
            if (coverNav == null)
            {
                Console.WriteLine("目录中似乎没有封面，尝试添加");
                epub.Nav.Insert(0, new NavItem { Title = "封面", Href = Util.ZipRelativePath(Path.GetDirectoryName(epub.GetNav().Href), epub.GetCoverXhtml().Href) });
            }
        }

        // 处理特殊样式
        private static void ProcessClass(IHtmlDocument doc)
        {
            doc.QuerySelectorAll("span.tcy").ForEach(span => span.OuterHtml = span.InnerHtml);
            doc.QuerySelectorAll("span.sideways").ForEach(span => span.OuterHtml = span.InnerHtml);
            doc.QuerySelectorAll(".gfont").ForEach(span =>
            {
                span.ClassList.Remove("gfont");
                if (span.ClassList.Length == 0) span.RemoveAttribute("class");
            });
            doc.QuerySelectorAll("span.font-1em10").ForEach(span =>
            {
                span.ClassList.Remove("font-1em10");
                if (span.ClassList.Length == 0 && span.ParentElement is IHtmlParagraphElement pNode && pNode.TextContent == span.TextContent)
                {
                    pNode.InnerHtml = span.TextContent;
                    pNode.ClassList.Add("em11");
                }
                else
                {
                    span.ClassList.Add("em11");
                }
            });
            doc.QuerySelectorAll("div.x---, div.imgc").ToArray().ForEach(div =>
            {
                // 疑似书(墮神契文)独有
                if (!div.HasChildNodes)
                {
                    div.Remove();
                    return;
                }
                div.RemoveAttribute("class");
                div.RemoveAttribute("xmlns");
            });
        }

        // 删除版权页，删除目录中的Nav，某些书书名页的位置和阅读顺序不相符，这里直接删掉
        private static async Task ProcessNav(EpubBook epub)
        {
            var nav = epub.GetNav();
            if (nav == null)
            {
                Console.WriteLine("本epub无法找到目录，跳过目录处理");
                return;
            }
            var last = epub.Nav.FirstOrDefault(c => c.Title == "版權頁");
            if (last != null)
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

                var match = Regex.Match("<p>插畫：(.*?)</p>", content);
                if (match.Success && match.Groups.Count > 0)
                {
                    IllusAuthor = match.Groups[0].Value;
                }

                epub.DeleteItem(id);
                last.Remove();
            }
            // 假如Nav和Spine文件包含自身，则删除
            var fileName = Path.GetFileName(nav.Href);
            epub.Nav.FirstOrDefault(c => c.Href == fileName)?.Remove();
            epub.Package.Spine.FirstOrDefault(c => c.IdRef == nav.ID)?.Remove();
            // 某些书书名页和目录页顺序不对，这里尝试删除书名页
            var smNav = epub.Nav.FirstOrDefault(x => x.Title == "書名頁");
            smNav?.Remove();
        }

        // 简单实现一个相似度算法
        public static double Sim(string txt1, string txt2)
        {
            List<char> sl1 = txt1.ToCharArray().ToList();
            List<char> sl2 = txt2.ToCharArray().ToList();
            //去重
            List<char> sl = sl1.Union(sl2).ToList<char>();

            //获取重复次数
            List<int> arrA = new List<int>();
            List<int> arrB = new List<int>();
            foreach (var str in sl)
            {
                arrA.Add(sl1.Where(x => x == str).Count());
                arrB.Add(sl2.Where(x => x == str).Count());
            }
            //计算商
            double num = 0;
            //被除数
            double numA = 0;
            double numB = 0;
            for (int i = 0; i < sl.Count; i++)
            {
                num += arrA[i] * arrB[i];
                numA += Math.Pow(arrA[i], 2);
                numB += Math.Pow(arrB[i], 2);
            }
            double cos = num / (Math.Sqrt(numA) * Math.Sqrt(numB));
            return cos;
        }

        // 尝试从epub目录找到对应的正文，并改为h4标签
        private static async Task ProcessTitle(EpubBook epub)
        {
            // 为了兼容某些书章节名和目录名部分符号不同
            string strProcess(string str)
            {
                str = str.Replace("\u3000", string.Empty);
                str = str.Replace(" ", string.Empty);
                str = str.Replace("：", string.Empty);
                return str.Trim(EmptyChar);
            };

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

                IHtmlCollection<IElement> iter;
                if (doc.Body.FirstElementChild.ChildElementCount > 3) iter = doc.Body.FirstElementChild.Children;
                else iter = doc.QuerySelectorAll("p");

                foreach (var pNode in iter.Take(30))
                {
                    // 为了兼容某些书章节名带注音
                    var node = (IElement)pNode.Clone();
                    node.QuerySelectorAll("rt").ToArray().ForEach(c => c.Remove());
                    // 有的书章节数字采用格式不同，这里使用一个简单的相似度，但有局限，需要看情况开启
                    //if (strProcess(node.TextContent) == strProcess(item.Title))
                    if (Sim(strProcess(node.TextContent), strProcess(item.Title)) > 0.7)
                    {
                        RemoveEmptyParagraphElement(pNode.NextElementSibling);
                        if (pNode.OuterHtml.Contains(item.Title)) pNode.OuterHtml = $"<h4>{item.Title}</h4>";
                        else pNode.OuterHtml = $"<h4>{pNode.InnerHtml}</h4>";
                        break;
                    }
                }

                await epub.SetItemContentByIDAsync(id, doc.ToXhtml());
            }
        }

        // 清理元数据
        private static void ProcessPackage(EpubBook epub)
        {
            var cover = epub.Cover;

            // 删除没用的属性
            epub.Package.BaseElement.Attribute("prefix")?.Remove();
            epub.Package.BaseElement.Attribute("unique-identifier")?.SetValue("BookId");

            epub.Package.Spine.BaseElement.Attribute("page-progression-direction")?.Remove();
            epub.Package.Spine.ToArray().ForEach(item => item.BaseElement.Attribute("properties")?.Remove());
            foreach (var item in epub.Package.Manifest)
            {
                if (item.IsCover || item.IsNav) continue;
                item.BaseElement.Attribute("properties")?.Remove();
            }
            epub.Package.Metadata.BaseElement.Elements(EpubBook.OpfNs + "meta").ToArray().ForEach(c => c.Remove());

            epub.Package.Metadata.BaseElement.Elements()
                .Where(c => new[] { "bw-ecode", "publisher" }.Contains(c.Attribute("id")?.Value))
                .ToArray()
                .ForEach(c => c.Remove());

            var author = epub.Author;
            if (string.IsNullOrEmpty(author)) author = epub.Creator;
            epub.Package.Metadata.BaseElement.Elements(EpubBook.DcNs + "creator").ToArray().ForEach(c => c.Remove());
            epub.Creator = "无语";
            epub.Author = author;
            if (!string.IsNullOrEmpty(cover)) epub.Cover = cover;
        }

        // 在封面后面添加制作信息和设置彩页目录，并尝试删除自带的Logo页
        private static async Task AddMessageAndIllus(EpubBook epub)
        {
            var title = epub.Package.Manifest.FirstOrDefault(x => x.Href.Contains("p-titlepage.xhtml"));
            if (title != null)
            {
                var content = await epub.GetItemContentByIDAsync(title.ID);
                if (content.Contains("Images/logo")) epub.DeleteItem(title.ID);
            }

            // 添加制作信息
            var message = await File.ReadAllTextAsync("Script/Res/message.xhtml");
            epub.AddItem(new EpubItem
            {
                Data = Encoding.UTF8.GetBytes(string.Format(message, epub.Author, IllusAuthor)), // 此处填写插画师，在插画师处理插件中补全
                EntryName = "Text/message.xhtml",
                ID = "message.xhtml"
            });
            var spine = epub.Package.Spine.FirstOrDefault(c => c.IdRef == "message.xhtml");
            var cover = epub.GetCoverXhtml();
            if (cover == null)
            {
                var coverNav = epub.Nav.FirstOrDefault(c => c.Title == "封面");
                if (coverNav != null)
                {
                    var coverHref = Util.ZipResolvePath(Path.GetDirectoryName(epub.GetNav().Href), coverNav.Href);
                    cover = epub.Package.Manifest.FirstOrDefault(x => x.Href == coverHref);
                }
                if (cover == null)
                {
                    Console.WriteLine("没有找到封面页，跳过制作信息和彩页处理");
                    return;
                }
            }
            var coverSpine = epub.Package.Spine.FirstOrDefault(c => c.IdRef == cover.ID);
            var coverIndex = epub.Package.Spine.IndexOf(coverSpine);
            epub.Package.Spine.Remove(spine);
            epub.Package.Spine.Insert(coverIndex + 1, spine);
            var nav = epub.GetNav();
            if (nav != null)
            {
                var coverNav = epub.Nav.FirstOrDefault(x => x.Title == "封面");
                if (coverNav != null)
                {
                    var messageNav = new NavItem
                    {
                        Href = Util.ZipRelativePath(Path.GetDirectoryName(nav.Href), "Text/message.xhtml"),
                        Title = "製作信息"
                    };
                    coverNav.BaseElement.AddAfterSelf(messageNav.BaseElement);

                    // 设置彩页
                    if (!epub.Nav.Any(x => x.Title.StartsWith("彩頁")))
                    {
                        var id = epub.Package.Spine[coverIndex + 2].IdRef;
                        var href = epub.GetEntryName(id);
                        if (!epub.Nav.Any(x => x.Href == Util.ZipRelativePath(Path.GetDirectoryName(nav.Href), href)))
                        {
                            var illusNav = new NavItem { Href = Util.ZipRelativePath(Path.GetDirectoryName(nav.Href), href), Title = "彩頁" };
                            messageNav.BaseElement.AddAfterSelf(illusNav.BaseElement);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("本书似乎没有彩页，跳过彩页处理");
                        }
                    }
                    else
                    {
                        var list = epub.Nav.Where(x => Regex.IsMatch(x.Title, "彩頁\\d+")).ToArray();
                        if (list.Length > 1)
                        {
                            Console.WriteLine("目录中有多个彩页，删除多余的");
                            list.ForEach(x =>
                            {
                                if (x.Title == "彩頁1") x.Title = "彩頁";
                                else x.Remove();
                            });
                        }
                        else
                        {
                            Console.WriteLine("目录中似乎已经有彩页了，跳过彩页处理");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("没有找到封面，跳过制作信息和彩页处理");
                }
            }
            else
            {
                Console.WriteLine("没有找到目录，跳过制作信息和彩页处理");
            }
        }
    }
}
