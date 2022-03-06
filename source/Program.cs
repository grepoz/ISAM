using ISAM.source;
using System;

//
// author: Grzegorz Pozorski
//

namespace ISAM
{
    internal class Program
    {
        protected Program(){}
        private static void Main()
        {
            var fm = new FileMenager();
            var dbms = new Dbms(fm);

            MenageCommands(true, dbms, fm);

            Console.WriteLine("END OF PROGRAM!");
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

        private static void MenageCommands(bool isInputFromConsole, Dbms dbms, FileMenager fm)
        {

            if (isInputFromConsole)
            {
                while (true)
                {
                    DispInstruction();
                    var cmd = Console.ReadLine();
                    if (cmd == "Q") break;

                    dbms.CmdHandler(new[] { cmd });
                }
            }
            else
            {
                var cmds = fm.ReadTestFile();
                dbms.CmdHandler(cmds.ToArray());
            }

        }
    }
}
