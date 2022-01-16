using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISFO.source
{
    class Experiment
    {

        public void ConductExperiment(string operation, int nrOfCmds)
        {

            GenerateInsertCmds(nrOfCmds, mode: "a");


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
                for (int i = 0; i < nrOfCmds; i++)
                {
                    cmds.Add($"I {Math.Ceiling(rnd.Next() / 1000.0)} 1 1");
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
