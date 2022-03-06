using System;

namespace ISAM.source
{
    internal class Record
    {
        private int _key, _data1, _data2;
        private int _deleted;    // 0 - false, 1 - true
        private int _next;       // index of next record, if equals '-1' its empty

        public Record(int key, int data1, int data2, int deleted = 0, int next = -1)
        {
            _key = key;
            _data1 = data1;
            _data2 = data2;
            _deleted = deleted;
            _next = next;
        }

        public Record()
        {
            _key = _data1 = _data2 = _deleted = 0;
            _next = -1;
        }

        public Record(Record sourceRecord)
        {
            Copy(sourceRecord);
        }

        public override string ToString() {
            var deletedChar = (_deleted == 0) ? " " : "D";
            var nextChar = (_next == -1) ? " " : _next.ToString();
            return $"{deletedChar}|[key:{_key}, data: ({_data1}, {_data2}), next: {nextChar}]\n";
        } 

        public int[] ToIntArr() => new[] { _key, _data1, _data2, _deleted, _next };

        public int GetKey() => _key;

        public int GetData1() => _data1;

        public int GetData2() => _data1;

        public int GetNext() => _next;
        public void SetNext(int next)
        {
            if (next < -1)
                throw new InvalidOperationException("Wrong values!");
            _next = next;
        }

        public int GetDeleted() => _deleted;
        public void SetDeleted(int deleted)
        {
            if (deleted != 0 && deleted != 1)
                throw new InvalidOperationException("Wrong values!");
            _deleted = deleted;
        }

        public bool IsEmpty()
        {
            return (_key == 0 && _data1 == 0 && _data2 == 0 && _deleted == 0 && _next == -1);
        }

        public bool HasNext() => _next != -1;

        public void WriteRecToFile(string filePath, int indexInOverflow)
        {
            FileMenager.WriteToFile(filePath, new[] { this }, indexInOverflow * Dbms.R);
        }

        public void Update(Record freshRecord)
        {
            _key = freshRecord._key;
            _data1 = freshRecord._data1;
            _data2 = freshRecord._data2;
        }

        public void Copy(Record toCopy)
        {
            _key = toCopy._key;
            _data1 = toCopy._data1;
            _data2 = toCopy._data2;
            _deleted = toCopy._deleted;
            _next = toCopy._next;
        }

        public void Delete()
        {
            _deleted = 1;
        }

        public void Clear()
        {
            _key = _data1 = _data2 = _deleted = 0;
            _next = -1;
        }

        public bool Exist() => _deleted == 0;

    }
}
