using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Wuyu.Epub;

namespace EpubProcess
{
    /// <summary>
    /// 执行简单的替换
    /// </summary>
    public class ExampleReg : Script
    {
        public override async Task<int> ParseAsync(EpubBook epub)
        {
            foreach (var id in epub.GetTextIDs())
            {
                var stream = epub.GetItemByID(id);
                using var streamReader = new StreamReader(stream);
                var content = await streamReader.ReadToEndAsync();

                content = Regex.Replace(content, "妳", "你");

                await using var streamWrite = new StreamWriter(stream);
                streamWrite.BaseStream.SetLength(0);
                await streamWrite.WriteAsync(content);
            }
            return 0;
        }
    }
}
