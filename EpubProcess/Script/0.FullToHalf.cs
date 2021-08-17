using System;
using System.IO;
using System.Threading.Tasks;
using Wuyu.Epub;
using System.Text.RegularExpressions;

namespace EpubProcess
{
    /// <summary>
    /// 全角转半角
    /// </summary>
    public class FullToHalf : Script
    {
        public override async Task<int> ParseAsync(EpubBook epub)
        {
            foreach (var id in epub.GetTextIDs())
            {
                var stream = epub.GetItemStreamByID(id);
                using var streamReader = new StreamReader(stream, System.Text.Encoding.UTF8);
                var content = ToDBC(await streamReader.ReadToEndAsync());
                await using var streamWrite = new StreamWriter(stream, System.Text.Encoding.UTF8);
                streamWrite.BaseStream.SetLength(0);
                await streamWrite.WriteAsync(content);
            }
            return 0;
        }

        public static string ToDBC(string input)
        {
            return Regex.Replace(input, "([ａ-ｚＡ-Ｚ０-９]{1})", new MatchEvaluator(m => ((char)(m.Groups[1].Value[0] - 65248)).ToString()));
            //char[] c = input.ToCharArray();
            //for (int i = 0; i < c.Length; i++)
            //{
            //    if (c[i] == 12288)
            //    {
            //        c[i] = (char)32;
            //        continue;
            //    }
            //    if (c[i] > 65280 && c[i] < 65375)
            //        c[i] = (char)(c[i] - 65248);
            //}
            //return new string(c);
        }
    }
}
