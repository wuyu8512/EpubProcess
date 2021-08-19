using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpubProcess;
using Wuyu.Epub;

namespace EpubProces
{
    class 插画师处理 : Script
    {
        private static readonly (string key, string value)[] ReplaceString = new[]
        {
            ("腦漿炸裂Girl","ちゃつぽ"), // 左边书名 右边插画师名称
        };

        public override async Task<int> ParseAsync(EpubBook epub)
        {
            var content = await epub.GetItemContentByIDAsync("message.xhtml");
            foreach (var item in ReplaceString)
            {
                if (epub.Title.Contains(item.key))
                {
                    content = string.Format(content, item.value);
                    await epub.SetItemContentByIDAsync("message.xhtml", content);
                    break;
                }
            }
            return 0;
        }
    }
}
