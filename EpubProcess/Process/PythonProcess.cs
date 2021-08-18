using Microsoft.Scripting.Hosting;
using Python.Runtime;
using System;
using System.Threading.Tasks;
using Wuyu.Epub;

namespace EpubProcess.Process
{
    class PythonProcess : BaseProcess
    {
        private static readonly Lazy<ScriptEngine> Engine = new Lazy<ScriptEngine>(IronPython.Hosting.Python.CreateEngine);
        public override string[] Extension => new[] { ".py" };
        
        public override async Task<int> ExecuteAsync(string script, EpubBook epub)
        {
            //var scope = Engine.Value.CreateScope();
            //Engine.Value.Execute(script, scope);
            //var run = scope.GetVariable("run");
            //run(epub);

            using (var state = Py.GIL())
            {
                using var scope = Py.CreateScope();
                using var pyEpub = epub.ToPython();
                scope.Set("epub", pyEpub);
                scope.Exec(script);
            }

            return 0;
        }
    }
}
