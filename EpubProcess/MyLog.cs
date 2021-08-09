using AngleSharp.Js;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpubProcess
{
    class MyLog : IConsoleLogger
    {
        public void Log(object[] values)
        {
            foreach (var value in values)
            {
                Console.WriteLine(value);
            }
        }
    }
}
