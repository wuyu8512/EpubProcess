using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Xhtml;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuyu.Epub;
 
namespace EpubProcess
{
    class EpubParse : Script
    {
        private static readonly HtmlParser Parser = new();

        public override async Task<int> ParseAsync(EpubBook epub)
        {
            // 删掉所有的Style文件，用默认的替换
            var cssIds = epub.GetItemIDs(new[] { ".css" }).ToArray();
            foreach (var id in cssIds)
            {
                epub.DeleteItem(id);
            }
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
                var stream = epub.GetItemByID(id);
                using var streamReader = new StreamReader(stream, Encoding.UTF8);
                var content = await streamReader.ReadToEndAsync();

                var doc = await Parser.ParseDocumentAsync(content);

                ProcessImage(doc);
                RemoveEmptyParagraphElement(doc);

                await using var streamWrite = new StreamWriter(stream, Encoding.UTF8);
                streamWrite.BaseStream.SetLength(0);
                doc.ToHtml(streamWrite, XhtmlMarkupFormatter.Instance);
            }
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
            var svgs = doc.QuerySelectorAll("svg");
            foreach (var svg in svgs)
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
            foreach (var pNode in doc.QuerySelectorAll("p"))
            {
                if (pNode.ChildElementCount != 1) continue;
                if (pNode.FirstElementChild is IHtmlImageElement pImg)
                {
                    var src = pImg.GetAttribute("src");

                    var div = doc.CreateElement("div");
                    div.ClassName = "illus duokan-image-single";

                    var img = doc.CreateElement("img");
                    img.SetAttribute("alt", Path.GetFileNameWithoutExtension(src));
                    img.SetAttribute("src", src);
                    div.AppendChild(img);

                    pNode.OuterHtml = pNode.ToXhtml();
                }
            }
        }
    }
}
