using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuyu.Epub;

namespace EpubProcess
{
    public abstract class Script
    {
        public abstract Task<int> ParseAsync(EpubBook epub);
    }
}
