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

        public override string ToString() => $"[ key:{key}, data: ({data1}, {data2}), deleted: {deleted} ]\n";

        public int[] ToIntArr() => new int[] { key, data1, data2, deleted, next };

        public bool IsEmpty()
        {
            return (key == 0 && data1 == 0 && data2 == 0 && deleted == 0 && next == -1);
        }

    }
}
