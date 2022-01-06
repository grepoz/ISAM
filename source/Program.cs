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

            List<Record> testRecords = new List<Record>();
            int nrOfTestRecs = 2;
            for (int i = 0; i < nrOfTestRecs; i++)
            {
                testRecords.Add(new Record(nrOfTestRecs - i , (i + 1) * 2, (i + 1) * 4));
            }
            int cnt = 0;
            foreach (var record in testRecords)
            {
                dbms.InsertRecord(record);
                Console.WriteLine("Insert " + ++cnt);
                fm.DisplayFileContent(FileMenager.GetPrimaryFileName());
                fm.DisplayFileContent(FileMenager.GetOverflowFileName());

            }

            Console.WriteLine($"Found: {dbms.GetRecord(keyOfRecToFound: 1)}");

            Console.WriteLine("After update!");
            fm.DisplayFileContent(FileMenager.GetPrimaryFileName());
            fm.DisplayFileContent(FileMenager.GetOverflowFileName());

            //List<string> commands = fm.ReadTestFile();

            Console.ReadKey();
        }
    }
}
