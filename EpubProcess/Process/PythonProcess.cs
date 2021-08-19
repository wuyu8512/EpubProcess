using Microsoft.Scripting.Hosting;
using Python.Runtime;
using System;
using System.Threading.Tasks;
using Wuyu.Epub;

namespace EpubProcess.Process
{
    class PythonProcess : BaseProcess
    {
        public override string[] Extension => new[] { ".py" };

        public override async Task<int> ExecuteAsync(string script, EpubBook epub)
        {
            PythonEngine.Initialize();
            Console.WriteLine(PythonEngine.IsInitialized);
            var pyEpub = epub.ToPython();
            var obj = PyModule.FromString("aaa", script);
            Console.WriteLine(obj);
            obj.InvokeMethod("run", pyEpub);

            //PythonEngine.Exec(script);

            PythonEngine.Shutdown();

            return 0;
        }
    }
}
