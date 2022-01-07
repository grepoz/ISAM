using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISFO.source
{
    class Record
    {
        public int key, data1, data2;
        public int deleted;    // 0 - false, 1 - true
        public int next;       // index of next record, if equals '-1' its empty

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

        public bool IsEmpty()
        {
            return (key == 0 && data1 == 0 && data2 == 0 && deleted == 0 && next == -1);
        }

        public bool HasNext()
        {
            return next != -1;
        }

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

    }
}
