using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISFO
{
    class MyFile
    {
        private readonly string filePath;

        public unsafe struct Record
        {
            int key { get; }
            (int, int) tup { get; }
            byte deleted { get; }
            int* next { get; }

            public Record(int key, (int, int) tup)
            {
                this.key = key;
                this.tup = tup;
                deleted = 0;
                next = null;
            }

            public override string ToString() => $"[ key:{key}, data: ({tup.Item1}, {tup.Item1}), deleted: {deleted} ]";

        }

        private List<Record> records;

        public MyFile(string filePath, List<Record> records)
        {
            this.filePath = filePath;
            this.records = records;
        }
    }
}
