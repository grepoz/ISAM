using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISFO.source
{
    class Experiment
    {
        public void ConductExperiment(int nrOfCmds, string mode)
        {
            List<string> cmds = GenerateInsertCmds(nrOfCmds, mode);
            double[] alphaValues = { 0.25, 0.5, 0.75 };
            double[] deltaValues = { 0.25, 0.5, 0.75 };

            FileMenager fm = new FileMenager();
            DBMS dbms = new DBMS(fm, isDebug: false);
            dbms.SetParametersDynamically(alphaValues[2], deltaValues[1]);

            dbms.CmdHandler(cmds.ToArray());
            Console.WriteLine($"operations: {DBMS.nrOfOperations}, file size: {FileMenager.GetFileSize()}");

            //List<(int T, long S)> results = new List<(int T, long S)>();

            //for (int i = 0; i < alphaValues.Length; i++)
            //{
            //    for (int j = 0; j < deltaValues.Length; j++)
            //    {
            //        FileMenager fm = new FileMenager();
            //        DBMS dbms = new DBMS(fm, isDebug: false);
            //        dbms.SetParametersDynamically(alphaValues[i], deltaValues[j]);

            //        dbms.CmdHandler(cmds.ToArray());

            //        results.Add((DBMS.nrOfOperations, FileMenager.GetFileSize()));

            //        dbms.DisplayIndexFileContent(fm.GetIndexFileName());
            //        dbms.DisplayFileContent(fm.GetPrimaryFileName());
            //        dbms.DisplayFileContent(fm.GetOverflowFileName());

            //        Console.WriteLine($"N: {DBMS.N}");
            //        Console.WriteLine($"V: {DBMS.V}");

            //        DBMS.ResetStaticValues();
            //        fm.DeleteFiles();

            //    }
            //}

            //foreach (var result in results)
            //{
            //    Console.WriteLine($"operations: {result.T}, file size: {result.S}");
            //}


        }

        public List<string> GenerateInsertCmds(int nrOfCmds, string mode)
        {
            // thera are 3 modes: a (ascending), b (descending), r (random)
            // data is not important - fill with '1'
            if (nrOfCmds < 1) throw new InvalidOperationException("Cannot generate commands!");

            Random rnd = new Random();
            List<string> cmds = new List<string>();

            if (mode == "a")
            {
                for (int i = 0; i < nrOfCmds; i++)
                {
                    cmds.Add($"I {i + 1} 1 1");
                }
            }
            else if (mode == "d")
            {
                for (int i = 0; i < nrOfCmds; i++)
                {
                    cmds.Add($"I {nrOfCmds - i} 1 1");
                }
            }
            else if (mode == "r")
            {
                List<int> numbers = new List<int>();
                for (int i = 0; i < nrOfCmds; i++)
                {
                    int rndNr;
                    while (true) {
                        rndNr = (int)Math.Ceiling(rnd.Next() / 1000.0);
                        if (!numbers.Contains(rndNr))
                        {
                            numbers.Add(rndNr);
                            break;
                        }
                     }

                    cmds.Add($"I {rndNr} 1 1");
                }
            }
            else
            {
                throw new InvalidOperationException("Wrong mode!");
            }


            return cmds;
        }

        public List<string> GenerateUpdateCmds(int nrOfCmds)
        {
            if (nrOfCmds < 1) throw new InvalidOperationException("Cannot generate commands!");

            Random rnd = new Random();
            List<string> cmds = new List<string>();
            for (int i = 0; i < nrOfCmds; i++) 
            {
                cmds.Add($"U {Math.Ceiling(rnd.Next() / 1000.0)} 2 2");
            }

            return cmds;
        }

    }
}
