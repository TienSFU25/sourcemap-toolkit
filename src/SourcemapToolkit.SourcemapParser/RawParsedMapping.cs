using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourcemapToolkit.SourcemapParser
{
    public class RawParsedMapping
    {
        public int GenSrcCol;
        public int? OrigSrcLine;
        public int? OrigSrcCol;
        public int? OrigFileIndex;
        public int? OrigNameIndex;
    }
}
