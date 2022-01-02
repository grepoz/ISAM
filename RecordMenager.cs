using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ISFO.MyFile;

namespace ISFO
{
    class RecordMenager
    {

        public static bool CheckRecordFormat(string record)
        {
            Regex rx = new Regex("^([0-9]+) ([0-9]+) ([0-9]+)$");
            MatchCollection matches = rx.Matches(record);
            return matches.Count > 0;
        }

        
        public static bool CheckTestRecordFormat(string testRecord)
        {
            Regex rx = new Regex("^(INS|AKT|DEL) ([0-9]+) ([0-9]+) ([0-9]+)$");
            MatchCollection matches = rx.Matches(testRecord);
            return matches.Count > 0;
        }

        public static Record ParseStrToRecord(string userInput)
        {
            // method retrives record from string 
            string[] strNrs = userInput.Split(' ');
            uint key;
            int r, h;      
            if (!UInt32.TryParse(strNrs[0], out key)) key = UInt32.MaxValue;
            if (!Int32.TryParse(strNrs[1], out r)) r = Int32.MaxValue;
            if (!Int32.TryParse(strNrs[2], out h)) h = Int32.MaxValue;

            return new Record(key, (r,h));
        }

    }
}
