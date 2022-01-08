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
            DBMS dbms = new DBMS(fm);

            List<Record> testRecords = new List<Record>();
            const int nrOfTestRecs = 5;
            for (int i = 0; i < nrOfTestRecs; i++)
            {
                testRecords.Add(new Record(nrOfTestRecs - i , (i + 1) * 2, (i + 1) * 4));
            }
            int cnt = 0;
            foreach (var record in testRecords)
            {
                dbms.InsertRecord(record);
                Console.WriteLine($"\nInsert {++cnt}\n" );
                dbms.DisplayFileContent(fm.GetPrimaryFileName());
                dbms.DisplayFileContent(fm.GetOverflowFileName());

            }

            Console.WriteLine($"Found: {dbms.ReadRecord(key: 7)}");

            Console.WriteLine($"nr of primary records: {DBMS.N}");
            Console.WriteLine($"nr of overflow records: {DBMS.V}");

            dbms.DisplayDBAscending();

            dbms.Reorganise();

            //List<string> commands = fm.ReadTestFile();

            Console.ReadKey();
        }
    }
}
