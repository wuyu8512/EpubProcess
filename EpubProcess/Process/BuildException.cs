using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpubProcess.Process
{
    class BuildException : Exception
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public BuildException(ImmutableArray<Diagnostic> diagnostics): base()
        {
            this.Diagnostics = diagnostics;
        }
    }
}
