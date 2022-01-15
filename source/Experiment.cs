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

            GenerateInputCmds(nrOfCmds);


        }

        public List<string> GenerateInputCmds(int nrOfCmds)
        {
            // data is not important - fill with '1'
            if (nrOfCmds < 1) throw new InvalidOperationException("Cannot generate commands!");

            Random rnd = new Random();
            List<string> cmds = new List<string>();
            for (int i = 0; i < nrOfCmds; i++)
            {
                cmds.Add($"I {Math.Ceiling(rnd.Next() / 1000.0)} 1 1");
            }
            return cmds;
        }


    }
}
