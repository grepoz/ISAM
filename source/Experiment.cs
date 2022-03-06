using System;
using System.Collections.Generic;

namespace ISAM.source
{
    internal class Experiment
    {
        public void ConductExperiment(int nrOfCmds, string mode)
        {
            var cmds = GenerateInsertCmds(nrOfCmds, mode);
            double[] alphaValues = { 0.25, 0.5, 0.75 };
            double[] deltaValues = { 0.25, 0.5, 0.75 };

            var fm = new FileMenager();
            var dbms = new Dbms(fm, isDebug: false);
            dbms.SetParametersDynamically(alphaValues[2], deltaValues[2]);

            dbms.CmdHandler(cmds.ToArray());
            Console.WriteLine($"operations: {Dbms.nrOfOperations}, file size: {FileMenager.GetFileSize()}, nr of reorg: {Dbms.nrOfReorg}");
        }
        public List<string> GenerateInsertCmds(int nrOfCmds, string mode)
        {
            // thera are 3 modes: a (ascending), b (descending), r (random)
            // data is not important - fill with '1'
            if (nrOfCmds < 1) throw new InvalidOperationException("Cannot generate commands!");

            var rnd = new Random();
            var cmds = new List<string>();

            switch (mode)
            {
                case "a":
                {
                    for (var i = 0; i < nrOfCmds; i++)
                    {
                        cmds.Add($"I {i + 1} 1 1");
                    }

                    break;
                }
                case "d":
                {
                    for (var i = 0; i < nrOfCmds; i++)
                    {
                        cmds.Add($"I {nrOfCmds - i} 1 1");
                    }

                    break;
                }
                case "r":
                {
                    var numbers = new List<int>();
                    for (var i = 0; i < nrOfCmds; i++)
                    {
                        int rndNr;
                        while (true) {
                            rndNr = (int)Math.Ceiling(rnd.Next() / 1000.0);

                            if (numbers.Contains(rndNr)) continue;

                            numbers.Add(rndNr);
                            break;
                        }

                        cmds.Add($"I {rndNr} 1 1");
                    }

                    break;
                }
                default:
                    throw new InvalidOperationException("Wrong mode!");
            }

            return cmds;
        }

        public List<string> GenerateUpdateCmds(int nrOfCmds)
        {
            if (nrOfCmds < 1) throw new InvalidOperationException("Cannot generate commands!");

            var rnd = new Random();
            var cmds = new List<string>();
            for (var i = 0; i < nrOfCmds; i++) 
            {
                cmds.Add($"U {Math.Ceiling(rnd.Next() / 1000.0)} 2 2");
            }

            return cmds;
        }
    }
}
