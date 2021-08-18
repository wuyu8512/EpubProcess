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

namespace EpubProcess
{
    /// <summary>
    /// 文学少女_愛戀插話集 专用，必须在代码美化改进后运行
    /// </summary>
    public class 文学少女_愛戀插話集 : Script
    {
        private static readonly (string key, string value)[] ReplaceReg = new[]
        {
            ("石<img src=\"../images/cha.jpg\" */>","石圶"),
        };

        public override async Task<int> ParseAsync(EpubBook epub)
        {
            if (!epub.Title.Contains("愛戀插話集")) return 0;

            foreach (var item in epub.GetHtmlItems())
            {
                using var stream = epub.GetItemStreamByID(item.ID);
                using var streamReader = new StreamReader(stream);
                var content = await streamReader.ReadToEndAsync();

                // 正则替换
                ReplaceReg.ForEach((item) => content = Regex.Replace(content, item.key, item.value));

                await using (var streamWrite = new StreamWriter(stream))
                {
                    streamWrite.BaseStream.SetLength(0);
                    await streamWrite.WriteAsync(content);
                }

                if (item.IsNav) epub.UpDataNav();
            }
            return 0;
        }
    }
}
