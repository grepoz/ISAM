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
                testRecords.Add(new Record(nrOfTestRecs - i, (i + 1) * 2, (i + 1) * 4));
            }

            Experiment ex = new Experiment();

            //MenageCommands(IsInputFromConsole: true, dbms, fm);

            //dbms.DisplayDBAscending();

            //dbms.Reorganise();

            //dbms.DisplayFileContent(fm.GetIndexFileName());
            //dbms.DisplayFileContent(fm.GetPrimaryFileName());
            //dbms.DisplayFileContent(fm.GetOverflowFileName());

            Console.ReadKey();
        }

        private static void DispInstruction()
        {
            Console.WriteLine($"++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n" +
                $"Input command:\n" +
                $"- insert: 'I key data data' (example of insert: I 2 3 4)\n" +
                $"- update: 'U key newKey newData newData' (example of update: U 2 2 3 4)\n" +
                $"- delete: 'D key' (example od delete: D 2)\n" +
                $"- show record: 'S key' (example od show: S 2)\n" +
                $"- reorganise: 'REORG'\n" +
                $"- display file: 'DISP'\n" +
                $"- quit console: Q\n" +
                $"++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n");
        }

        private static void MenageCommands(bool IsInputFromConsole, DBMS dbms, FileMenager fm)
        {

            if (IsInputFromConsole)
            {
                while (true)
                {
                    DispInstruction();
                    string cmd = Console.ReadLine();
                    if (cmd == "Q") break;

                    dbms.CmdHandler(new[] { cmd });
                }
            }
            else
            {
                List<string> cmds = fm.ReadTestFile();
                dbms.CmdHandler(cmds.ToArray());
            }

        }
    }
}
