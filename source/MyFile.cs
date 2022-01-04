using ISFO.source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISFO
{
    class MyFile
    {
        private readonly string filePath;

        private List<Record> records;

        public MyFile(string filePath, List<Record> records)
        {
            this.filePath = filePath;
            this.records = records;
        }

        public static Record GetEmptyRecord()
        {
            return new Record();
        }

    }
}
