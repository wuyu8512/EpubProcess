using System;
using System.IO;
using System.Threading.Tasks;
using Wuyu.Epub;
using System.Text.RegularExpressions;

namespace EpubProcess
{
    public class FullToHalf : Script
    {
        public override async Task<int> ParseAsync(EpubBook epub)
        {
            foreach (var item in epub.GetHtmlItems())
            {
                using var stream = epub.GetItemStreamByID(item.ID);
                using var streamReader = new StreamReader(stream);

                var content = ToDBC(await streamReader.ReadToEndAsync());

                await using (var streamWrite = new StreamWriter(stream))
                {
                    streamWrite.BaseStream.SetLength(0);
                    await streamWrite.WriteAsync(content);
                }

                if (item.IsNav) epub.UpDataNav();
            }
            return 0;
        }

        /// <summary>
        /// 全角转半角
        /// </summary>
        public static string ToDBC(string input)
        {
            return Regex.Replace(input, "([ａ-ｚＡ-Ｚ０-９]{1})", new MatchEvaluator(m => ((char)(m.Groups[1].Value[0] - 65248)).ToString()));
        }
    }
}
