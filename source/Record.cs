using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISFO.source
{
    class Record
    {
        private int key, data1, data2;
        private int deleted;    // 0 - false, 1 - true
        private int next;       // index of next record, if equals '-1' its empty

        public Record(int key, int data1, int data2, int deleted = 0, int next = -1)
        {
            this.key = key;
            this.data1 = data1;
            this.data2 = data2;
            this.deleted = deleted;
            this.next = next;
        }

        public Record()
        {
            key = data1 = data2 = deleted = 0;
            next = -1;
        }

        public Record(Record sourceRecord)
        {
            Copy(sourceRecord);
        }

        public override string ToString() {
            string deletedChar = (deleted == 0) ? " " : "D";
            string nextChar = (next == -1) ? " " : next.ToString();
            //return $"{deletedChar}| [ key:{key}, data: ({data1}, {data2}), next: {nextChar} ]\n";
            return $"{deletedChar}| [ key:{key}, next: {nextChar} ]\n";
        } 

        public int[] ToIntArr() => new int[] { key, data1, data2, deleted, next };

        public int GetKey() => key;
        public void SetKey(int key)
        {
            if (key < 0)
                throw new InvalidOperationException("Wrong values!");
            this.key = key;
        }

        public int GetData1() => data1;
        public void SetData1(int data1)
        {
            if (data1 < 0)
                throw new InvalidOperationException("Wrong values!");
            this.data1 = data1;
        }

        public int GetData2() => data1;

        public void SetData2(int data2)
        {
            if (data2 < 0)
                throw new InvalidOperationException("Wrong values!");
            this.data2 = data2;
        }

        public int GetNext() => next;
        public void SetNext(int next)
        {
            if (next < 0)
                throw new InvalidOperationException("Wrong values!");
            this.next = next;
        }

        public int GetDeleted() => deleted;
        public void SetDeleted(int deleted)
        {
            if (deleted != 0 && deleted != 1)
                throw new InvalidOperationException("Wrong values!");
            this.deleted = deleted;
        }

        public bool IsEmpty()
        {
            return (key == 0 && data1 == 0 && data2 == 0 && deleted == 0 && next == -1);
        }

        public bool HasNext() => next != -1;

        public void WriteRecToFile(string filePath, int indexInOverflow)
        {
            FileMenager.WriteToFile(filePath, new[] { this }, indexInOverflow * DBMS.R);
        }

        public void Update(Record freshRecord)
        {
            key = freshRecord.key;
            data1 = freshRecord.data1;
            data2 = freshRecord.data2;
            deleted = freshRecord.deleted;
            next = freshRecord.next;

        }

        public void Copy(Record toCopy)
        {
            Update(toCopy);
        }

        public void Delete()
        {
            deleted = 1;
        }

        public void Clear()
        {
            key = data1 = data2 = deleted = 0;
            next = -1;
        }

        public bool Exist() => deleted == 0;

    }
}
