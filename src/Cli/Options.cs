using Pytocs.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Cli
{
    public class Options
    {
        public bool Recursive { get; set; }
        public List<IPostProcessor> PostProcessors { get; set; } = new();

        public string[] Arguments = Array.Empty<string>();
    }
}
