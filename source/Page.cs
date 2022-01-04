using ISFO.source;
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

        public List<Record> GetRecords()
        {
            return records;
        }

        public int GetFullfillment()
        {
            return records.Count();
        }

        public void Clear()
        {
            records.Clear();
        }

        public void DisplayPageContent()
        {
            foreach (var record in records)
            {
                Console.WriteLine(record.ToString());
            }
        }

        internal bool IsEmpty()
        {
            return !records.Any();
        }

        internal void Update(Record toBeInserted)
        {
            records[FindIndex(toBeInserted)] = toBeInserted;
        }

        internal int FindIndex(Record record)
        {
            return records.FindIndex(rec => rec.key == record.key);
        }
    }
}
