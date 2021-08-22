using EpubProcess;
using System.Threading.Tasks;
using Wuyu.Epub;

namespace EpubProces
{
    class 插画师处理 : Script
    {
        private static readonly (string key, string value)[] ReplaceString = new[]
        {
            ("腦漿炸裂Girl","ちゃつぽ"), // 左边书名 右边插画师名称
            ("愛戀插話集","竹岡美穗"),
            ("岸邊露伴完全不嬉鬧","荒木飛呂彥"),
            ("墮神契文","竹官@CIMIX BARZ"),
            ("煉愛交替","KAWORU"),
            ("當戀愛成為交易的時候","櫻野露"),
            ("神明判決","亞果"),
            ("御神樂學園組曲","明菜"),
            ("春日坂高中漫画研究社","ヤマコ"),
            ("排球少年","古舘春一"),
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
