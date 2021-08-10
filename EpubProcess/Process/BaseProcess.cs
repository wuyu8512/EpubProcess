using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Wuyu.Epub;
using EpubProcess.Process;

namespace EpubProcess.Process
{
    public abstract class BaseProcess
    {
        public static IEnumerable<BaseProcess> Processes = new BaseProcess[] { /*new JavaScriptProcess(),*/ new CSharpProcess(), new PythonProcess() };

        public abstract string[] Extension { get; }

        public abstract Task<int> ExecuteAsync(string script, EpubBook epub);
    }
}
