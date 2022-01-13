using ISFO.source;
using System;
using System.Text.RegularExpressions;

namespace ISFO
{
    class RecordMenager
    {

        public static bool IsRecordFormatValid(string record)
        {
            Regex rx = new Regex("^([0-9]+) ([0-9]+) ([0-9]+)$");
            MatchCollection matches = rx.Matches(record);
            return matches.Count > 0;
        }

        
        public static bool IsTestRecordFormatValid(string testRecord)
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

            return new Record(key, r, h);
        }

        public static Page BytesToPage(byte[] chunk)
        {
            Page page = new Page();

            int[] intsArr = BytesToInts(chunk);
            //const int bytesInInt = 4;
            //int nrOfInts = chunk.Length / bytesInInt;
            //int nrOfRecords = nrOfInts / DBMS.nrOfIntsInRecord; // instead of 'DBMS.recPerPage' becouse we could read less than whole site

            for (int i = 0; i < DBMS.bf; i++)
            {
                int k = i * DBMS.nrOfIntsInRecord;
                page.ReplaceFirstEmpty(new Record(intsArr[k+ 0], intsArr[k + 1], intsArr[k + 2], intsArr[k + 3], intsArr[k + 4]));
            }

            return page;

        }

        public static int[,] BytesToIndexPage(byte[] chunk)
        {
            int[] intsArr = BytesToInts(chunk);
            const int bytesInInt = 4;
            int nrOfInts = chunk.Length / bytesInInt;

            const int cols = 2;
            int rows = nrOfInts / cols;
            var indexFileRecords = new int[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                indexFileRecords[i, 0] = intsArr[i * cols];
                indexFileRecords[i, 1] = intsArr[i * cols + 1];
            }

            return indexFileRecords;
        }

        private static int[] BytesToInts(byte[] chunk)
        {
            const int bytesInInt = 4;
            int nrOfInts = chunk.Length / bytesInInt;
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
