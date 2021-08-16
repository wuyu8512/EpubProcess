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
            if (element.ChildElementCount == 0 && string.IsNullOrWhiteSpace(element.TextContent)) return true;
            else if (element.ChildElementCount == 1 && element.FirstElementChild is IHtmlBreakRowElement) return true;
            return false;
        }
    }
}
