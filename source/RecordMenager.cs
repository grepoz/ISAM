using ISFO.source;
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

        public static unsafe Record ParseStrToRecord(string userInput)
        {
            // method retrives record from string (record: [key r h])
            string[] strNrs = userInput.Split(' ');  
            
            if (!Int32.TryParse(strNrs[0], out int key)) key = Int32.MaxValue;
            if (!Int32.TryParse(strNrs[1], out int r)) r = Int32.MaxValue;
            if (!Int32.TryParse(strNrs[2], out int h)) h = Int32.MaxValue;
            (int, int) data = (r, h);

            return new Record(key, data);
        }

        public static unsafe Page BytesToPage(byte[] chunk, int B)
        {
            Page page = new Page();

            int[] intsArr = BytesToInts(chunk, B);
            const int bytesInInt = 4;
            int nrOfInts = B / bytesInInt;

            for (int i = 0; i < nrOfInts; i++)
            {
                int k = i * DBMS.nrOfIntsInRecord;

                Record record = new Record(
                    intsArr[k+ 0], 
                    (intsArr[k + 1], intsArr[k + 2]), 
                    intsArr[k + 3], 
                    (int*)intsArr[k + 4]);

                page.Add(record);
            }

            return page;

        }

        private static int[] BytesToInts(byte[] chunk, int B)
        {
            const int bytesInInt = 4;
            int nrOfInts = B / bytesInInt;
            byte[] fourByte = new byte[bytesInInt];
            int[] intsArr = new int[nrOfInts];
            int intsCnt = 0, bytesCnt = 0;

            for (int i = 0; i < chunk.Length; i++)
            {
                fourByte[bytesCnt++] = chunk[i];

                if (bytesCnt == bytesInInt)
                {
                    bytesCnt = 0;
                    intsArr[intsCnt++] = BitConverter.ToInt32(fourByte, 0);

                }
            }

            return intsArr;

        }


    }
}
