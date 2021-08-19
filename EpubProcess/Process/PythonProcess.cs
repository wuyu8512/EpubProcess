using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuyu.Epub;
using Python.Runtime;
using System.Text.RegularExpressions;

namespace EpubProcess.Process
{
    class PythonProcess : BaseProcess
    {
        public override string[] Extension => new[] { ".py" };

        public PythonProcess()
        {
            var cmd = new System.Diagnostics.Process
            {
                StartInfo = {FileName = "python", RedirectStandardOutput = true, Arguments = " -V"}
            };
            cmd.Start();
            cmd.WaitForExit();
            var ver = cmd.StandardOutput.ReadToEnd();
            ver = Regex.Match(ver, "^Python (.*?)$").Groups[1].Value;
            if (OperatingSystem.IsWindows())
            {
                ver = "python" + string.Join(string.Empty, ver.Split(".").Take(2)) + ".dll";
            }
            else if (OperatingSystem.IsMacOS())
            {
                ver = "libpython" + string.Join(string.Empty, ver.Split(".").Take(2)) + ".dylib";
            }
            else if (OperatingSystem.IsLinux())
            {
                ver = "libpython" + string.Join(string.Empty, ver.Split(".").Take(2)) + ".so";
            }
            Runtime.PythonDLL = ver;
        }

        public override async Task<int> ExecuteAsync(string script, EpubBook epub)
        {
            await Task.Run(() =>
            {
                PythonEngine.Initialize();
                var code = PyModule.FromString("Code", script);
                var pyEpub = epub.ToPython();
                code.InvokeMethod("run", pyEpub);
                PythonEngine.Shutdown();
            });
            return 0;
        }
    }
}
