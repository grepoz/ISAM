using ISFO.source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISFO
{
    class Program
    {
        static void Main()
        {
            FileMenager fm = new FileMenager();
            DBMS dbms = new DBMS();

            fm.DisplayFileContent(FileMenager.GetPrimaryFileName());

            Record testRecord = new Record(2, 3, 4);

            dbms.InsertRecord(testRecord);

            //List<string> commands = fm.ReadTestFile();

            Console.ReadKey();
        }
    }
}
