using ISFO.source;
using System;
using System.Collections.Generic;
using System.Linq;


namespace ISFO
{
    class Page
    {
        Record[] records;

        public Page()
        {
            records = InitArrayOfRecords(DBMS.recPerPage);
        }

        public static Record[] InitArrayOfRecords(int length)
        {
            Record[] array = new Record[length];
            for (int i = 0; i < length; ++i)
                array[i] = new Record();
            return array;
        }

        public void ReplaceFirstEmpty(Record record)
        {
            for (int i = 0; i < records.Length; i++)
            {
                if(records[i].IsEmpty())
                {
                    records[i] = record;
                    return;
                }
            }
            throw new InvalidOperationException("Cannot replace empty record!");
        }

        public Record Get(int key)
        {
            return records.ElementAt(key);
        }

        public Record[] GetRecords()
        {
            return records;
        }

        public bool IsFull()
        {
            foreach (var record in records)
            {
                if (record.IsEmpty()) return false;
            }
            return true;
        }

        public void DisplayPageContent()
        {
            foreach (var record in records)
            {
                Console.WriteLine(record.ToString());
            }
        }

        internal void Update(Record toBeUpdated)
        {
            // change next!!!!!!!!!!!!
            records[FindIndex(toBeUpdated)] = toBeUpdated;
        }

        internal void UpdateData(Record toBeUpdated)
        {
            records[FindIndex(toBeUpdated)] = toBeUpdated;
        }

        internal int FindIndex(Record wantedRecord)
        {
            return Array.FindIndex(records, record => (record.key == wantedRecord.key));
        }
    }
}
