using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pros.Tailer
{
    class FileData
    {
        public string FileName { get; set; }
        public long Position { get; set; }
        public long CreateTicks { get; set; }
        public bool Changed { get; set; }
        public override string ToString()
        {
            return String.Format("\"{0}\" {1} {2}", FileName, CreateTicks, Position);
        }
    }
}
