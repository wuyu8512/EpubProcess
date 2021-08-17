using AngleSharp;
using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace EpubProcess
{
    public static class Extension
    {
        public static string ToXhtml(this IMarkupFormattable markupFormattable)
        {
            return markupFormattable.ToHtml(AngleSharp.Xhtml.XhtmlMarkupFormatter.Instance);
        }

        public static bool IsEmpty(this AngleSharp.Dom.IElement element)
        {
            if (element.ChildElementCount == 0 && element.TextContent.IsEmpty()) return true;
            else if (element.ChildElementCount == 1 && element.FirstElementChild is IHtmlBreakRowElement) return true;
            return false;
        }

        public static bool IsEmpty(this string str)
        {
            str = str?.Replace("\r", "").Replace("\n", "");
            return string.IsNullOrWhiteSpace(str);
        }

        public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (var item in list)
            {
                action(item);
            }
        }
    }
}
