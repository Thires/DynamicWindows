using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWindows
{
    public class StreamBox
    {
        public string Name { get; set; }
        public string Text { get; set; }

        public StreamBox(string name)
        {
            Name = name;
        }
    }
}
