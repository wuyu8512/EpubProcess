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
                var stream = epub.GetItemStreamByID(id);
                using var streamReader = new StreamReader(stream);
                var content = await streamReader.ReadToEndAsync();

                content = Regex.Replace(content, "妳", "你");
                content = Regex.Replace(content, "(?s)<.*<title.*?>(.*)</title>.*</head>", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<!DOCTYPE html>\r\n\r\n<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"zh-CN\" xmlns:epub=\"http://www.idpf.org/2007/ops\" xmlns:xml=\"http://www.w3.org/XML/1998/namespace\">\r\n<head>\r\n<title>${1}</title>\r\n<link href=\"../style/style.css\" type=\"text/css\" rel=\"stylesheet\"/>\r\n</head>");

                await using var streamWrite = new StreamWriter(stream);
                streamWrite.BaseStream.SetLength(0);
                await streamWrite.WriteAsync(content);
            }
            return 0;
        }
    }
}
