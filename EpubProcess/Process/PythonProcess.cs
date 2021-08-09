using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuyu.Epub;
using IronPython;
using Microsoft.Scripting.Hosting;

namespace EpubProcess.Process
{
    class PythonProcess : BaseProcess
    {
        private static readonly Lazy<ScriptEngine> Engine = new Lazy<ScriptEngine>(IronPython.Hosting.Python.CreateEngine);
        public override string[] Extension => new[] { ".py" };

        public override async Task<int> ExecuteAsync(string script, EpubBook epub)
        {
            var scope = Engine.Value.CreateScope();
            Engine.Value.Execute(script, scope);
            var run = scope.GetVariable("run");
            run(epub);
            return 0;
        }
    }
}
