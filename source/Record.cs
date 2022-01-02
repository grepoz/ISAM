using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISFO.source
{
    [Serializable()]
    unsafe class Record
    {

        int key;
        (int, int) data;
        int deleted;   // 0 - false, 1 - true
        int* next;

        public Record(int key, (int, int) data, int deleted = 0, int* next = null)
        {
            this.key = key;
            this.data = data;
            this.deleted = deleted;
            this.next = next;
        }

        public Record(int key = 0)
        {
            this.key = key;
            this.data = (0, 0);
            this.deleted = 0;
            this.next = null;
        }

        public override string ToString() => $"[ key:{key}, data: ({data.Item1}, {data.Item1}), deleted: {deleted} ]";

        private object ToTuple()
        {
            // is this cast correct
            return (this.key, this.data, this.deleted, (int)this.next);
        }

    }
}
