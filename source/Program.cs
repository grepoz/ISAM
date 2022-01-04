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

            fm.DisplayFileContent(FileMenager.GetPrimaryFileName());

            //List<string> commands = fm.ReadTestFile();

            Console.ReadKey();
        }
    }
}
