using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Polyphase_sort
{
    internal class Tape
    {
        public int Index { get; set; }
        public int BatchTotal { get; set; } //poslidovnosti
        public int BatchReal { get; set; }
        public int BatchEmpty { get; set; } 
        public bool IsEmpty { get; set; }
        public StreamWriter Writer { get; set; }
        public StreamReader Reader { get; set; }

        public Tape(int index)
        {
            Index = index;
            BatchTotal = BatchEmpty+BatchReal;
            BatchEmpty = 0;
            BatchReal = 0;
            IsEmpty = false;
            Writer = new StreamWriter($"temp_{index}");
            Reader = null;
        }
    }
}
