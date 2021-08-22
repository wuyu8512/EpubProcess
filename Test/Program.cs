using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var Config = Configuration.Default.WithCss();
            var Context = BrowsingContext.New(Config);
            var HtmlParser = Context.GetService<IHtmlParser>();

            var doc = HtmlParser.ParseDocument("<h4>第三章\u3000夢與超能力</h4>");
            Console.WriteLine(doc.DocumentElement.GetInnerText());
            Console.WriteLine(doc.DocumentElement.GetInnerText() == "第三章\u3000夢與超能力");
            Console.WriteLine(doc.DocumentElement.GetInnerText() == "第三章\u0020夢與超能力");
        }
    }
}
