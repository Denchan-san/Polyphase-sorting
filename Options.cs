using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polyphase_sort
{
    internal class Options
    {
        public string InputFileName { get; set; }
        public string OutputFileName { get; set; }
        public int TapesCount;
        public bool PreSort = false;
        public bool GenerateFiles = false;
    }
}
