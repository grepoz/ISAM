﻿using ISAM.source;
using System;
using System.Collections.Generic;

//
// Struktury baz danych - projekt 2
// author: Grzegorz Pozorski
// indeks: 180169
//

namespace ISAM
{
    class Program
    {
        static void Main()
        {
            bool isDebug = true;
            FileMenager fm = new FileMenager();
            DBMS dbms = new DBMS(fm, isDebug);

            MenageCommands(IsInputFromConsole: false, dbms, fm);

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
