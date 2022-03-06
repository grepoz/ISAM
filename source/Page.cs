using ISAM.source;
using System;
using System.Collections.Generic;
using System.Linq;


namespace ISAM
{
    internal class Page
    {
        private readonly Record[] _records;
        public int nr;
        public string pageFilePath;
        public Page()
        {
            _records = InitArrayOfRecords(Dbms.bf);
        }

        public static Record[] InitArrayOfRecords(int length)
        {
            var array = new Record[length];
            for (var i = 0; i < length; ++i)
                array[i] = new Record();
            return array;
        }

        public void ReplaceFirstEmpty(Record record)
        {
            for (var i = 0; i < _records.Length; i++)
            {
                if (!_records[i].IsEmpty()) continue;

                _records[i] = record;
                return;
            }
            throw new InvalidOperationException("Cannot replace empty record!");
        }

        public Record Get(int key)
        {
            return _records.FirstOrDefault(record => !record.IsEmpty() && record.GetKey() == key);
        }

        public Record[] GetRecords()
        {
            return _records;
        }

        public bool IsFull()
        {
            return _records.All(record => !record.IsEmpty());
        }

        public void DisplayPageContent()
        {
            Array.ForEach(_records, record => Console.WriteLine(record.ToString()));
        }

        public int MyGetLength()
        {
            int length = 0;
            foreach (var record in _records)
            {
                if (!record.IsEmpty()) length++;
            }
            return length;
        }

        internal int FindIndex(int key)
        {
            return Array.FindIndex(_records, record => (record.GetKey() == key));
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
