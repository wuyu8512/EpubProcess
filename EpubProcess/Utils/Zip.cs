using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpubProcess.Utils
{
    class ZipHelper
    {
        public static void UnZip(string zipPath, string outPath)
        {
            ZipFile.ExtractToDirectory(zipPath, outPath);
        }

        public static void Zip(string zipPath, string outPath)
        {
            ZipFile.CreateFromDirectory(outPath, zipPath, CompressionLevel.Fastest, false);
        }
    }
}
