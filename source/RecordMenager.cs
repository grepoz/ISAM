using ISAM.source;
using System;
using System.Text.RegularExpressions;
using static System.Int32;

namespace ISAM
{
    internal class RecordMenager
    {
        protected RecordMenager(){}
        public static bool IsRecordFormatValid(string record)
        {
            var rx = new Regex("^([0-9]+) ([0-9]+) ([0-9]+)$");
            var matches = rx.Matches(record);
            return matches.Count > 0;
        }

        public static bool IsTestRecordFormatValid(string testRecord)
        {
            var rx = new Regex("^(I|A|D) ([0-9]+) ([0-9]+) ([0-9]+)$");
            var matches = rx.Matches(testRecord);
            return matches.Count > 0;
        }

        public static Record ParseStrToRecord(string userInput)
        {
            var strNrs = userInput.Split(' ');  
            
            if (!TryParse(strNrs[0], out var key)) key = MaxValue;
            if (!TryParse(strNrs[1], out var r)) r = MaxValue;
            if (!TryParse(strNrs[2], out var h)) h = MaxValue;

            return new Record(key, r, h);
        }

        public static Page BytesToPage(byte[] chunk)
        {
            var page = new Page();

            var intsArr = BytesToInts(chunk);

            for (var i = 0; i < Dbms.bf; i++)
            {
                var k = i * Dbms.NrOfIntsInRecord;
                page.ReplaceFirstEmpty(new Record(intsArr[k+ 0], intsArr[k + 1], intsArr[k + 2], intsArr[k + 3], intsArr[k + 4]));
            }

            return page;
        }

        public static int[,] BytesToIndexPage(byte[] chunk)
        {
            var intsArr = BytesToInts(chunk);
            const int bytesInInt = 4;
            var nrOfInts = chunk.Length / bytesInInt;

            const int cols = 2;
            var rows = nrOfInts / cols;
            var indexFileRecords = new int[rows, cols];

            for (var i = 0; i < rows; i++)
            {
                indexFileRecords[i, 0] = intsArr[i * cols];
                indexFileRecords[i, 1] = intsArr[i * cols + 1];
            }

            return indexFileRecords;
        }

        private static int[] BytesToInts(byte[] chunk)
        {
            const int bytesInInt = 4;
            var nrOfInts = chunk.Length / bytesInInt;
            var fourByte = new byte[bytesInInt];
            var intsArr = new int[nrOfInts];
            int intsCnt = 0, bytesCnt = 0;

            foreach (var t in chunk)
            {
                fourByte[bytesCnt++] = t;

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
