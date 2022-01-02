using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ISFO.MyFile;

namespace ISFO
{
    class Page
    {
        List<Record> records;

        public Page()
        {
            records = new List<Record>();
        }

        public void Add(Record record)
        {
            records.Add(record);
        }

        public Record Get(int key)
        {
            return records.ElementAt(key);
        }

        public int GetFullfillment()
        {
            return records.Count();
        }

    }
}
