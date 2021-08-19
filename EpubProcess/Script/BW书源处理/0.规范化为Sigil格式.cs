using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuyu.Epub;
using System.Xml.Linq;

namespace EpubProcess
{
    /// <summary>
    /// 功能类似Sigil的规范化，具体是对文件结构重组，不好写，暂时保留
    /// </summary>
    class 规范化 : Script
    {
        public override async Task<int> ParseAsync(EpubBook epub)
        {
            foreach (var item in epub.Package.Manifest)
            {
                //using var stream = epub.GetItemStreamByID(item.ID);
                //var doc = XDocument.Load(stream);
            }

            return 0;
        }
    }
}
