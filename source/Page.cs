using ISAM.source;
using System;
using System.Collections.Generic;
using System.Linq;


namespace ISAM
{
    class Page
    {
        Record[] records;
        public int nr;
        public string pageFilePath;
        public Page()
        {
            records = InitArrayOfRecords(DBMS.bf);
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
            foreach (var record in records)
            {
                if (!record.IsEmpty())
                {
                    if (record.GetKey() == key)
                    {
                        return record;
                    }
                }
            }
            return null;
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
            Array.ForEach(records, record => Console.WriteLine(record.ToString()));
        }

        public int MyGetLength()
        {
            int length = 0;
            foreach (var record in records)
            {
                if (!record.IsEmpty()) length++;
            }
            return length;
        }

        internal void Update(int key, Record toBeUpdated)
        {
            // function updates whole every filed in record 
            // change next!!!!!!!!!!!!
            records[FindIndex(key)] = toBeUpdated;
        }

        internal void UpdateData(Record freshRecord)
        {
            Record toBeUpdated = records[FindIndex(freshRecord.GetKey())];
            toBeUpdated.SetData1(freshRecord.GetData1());
            toBeUpdated.SetData2(freshRecord.GetData2());
        }

        internal int FindIndex(int key)
        {
            return Array.FindIndex(records, record => (record.GetKey() == key));
        }

        public void Clear()
        {
            Array.ForEach(records, record => record.Clear());
        }

        public bool IsEmpty() => MyGetLength() == 0;

        public void SetPageNr(int pageNr)
        {
            nr = pageNr;
        }
        public void SetPageFilePath(string filePath)
        {
            pageFilePath = filePath;
        }
    }
}
