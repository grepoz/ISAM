using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISFO.source
{
    class DBMS
    {

        public const int B = 60;  // disk page capacity - nr of bytes readed at once - always must be multiple of 'R'
        private const int R = 20;   // size of record - 5* int 
        private const int K = 4;   // size of key - int 
        private const int P = 4;   // size of pointer - int 
        private const double alpha = 0.5;   // page utlilization factor in the main area just after reorganization, α < 1
        public const int defaultNrOfPages = 3;
        public const int nrOfIntsInRecord = R / 4;
        public const int recPerPage = B / R;
        public const double delta = 0.2;  // fullfillment of overflow ratio to fullfillment of primary 

        public static int nrOfPageInPrimary = defaultNrOfPages;
        public static int nrOfPageInOverflow = defaultNrOfPages;

        private int nextEmptyOverflowIndex = 0;

        public Page ReadPage(string filePath, int position)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(position, SeekOrigin.Begin);
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        if (br.BaseStream.Length > 0)
                        {
                            byte[] chunk = br.ReadBytes(B); 
                            return RecordMenager.BytesToPage(chunk);

                        }
                        else
                            return null;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot read a chunk from file:\n" + e.Message);
                return null;
            }
        }

        public int[,] ReadIndexPage(string filePath, int position)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(position, SeekOrigin.Begin);
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        if (br.BaseStream.Length > 0)
                        {
                            byte[] chunk = br.ReadBytes(B);
                            return RecordMenager.BytesToIndexPage(chunk);
                        }
                        else
                            return null;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot read a chunk from file:\n" + e.Message);
                return null;
            }
        }

        public void InsertRecord(Record toBeInserted)
        {
            ValidRecord(toBeInserted);

            int pageNr = GetPageNr(toBeInserted.key);

            Page page = ReadPage(FileMenager.GetPrimaryFileName(), (pageNr - 1) * B);

            string filePathToWriteTo = ChoosePlaceAndInsert(page, toBeInserted);

            FileMenager.WriteToFile(filePathToWriteTo, page.GetRecords(), (pageNr - 1) * B);

        }

        private string ChoosePlaceAndInsert(Page page, Record toBeInserted)
        {
            Record prevRecord = null;
            // if(
            foreach (var record in page.GetRecords())
            {
                if (record.IsEmpty())
                {
                    page.ReplaceFirstEmpty(toBeInserted);
                    return FileMenager.GetPrimaryFileName();
                }
                else if(record.key < toBeInserted.key)
                {
                    prevRecord = record;
                }
                else if (record.key > toBeInserted.key)
                {                   
                    InsertToOverflowFile(toBeInserted);
                    prevRecord.next = nextEmptyOverflowIndex++;     
                    return FileMenager.GetPrimaryFileName();
                }
                else // ==
                {
                    page.Update(toBeInserted);
                    return FileMenager.GetPrimaryFileName();
                }

            }
            // if not added to overflow nor updated - append

            if (page.IsFull())
            {
                InsertToOverflowFile(toBeInserted);


                if (prevRecord == null) throw new InvalidProgramException("Uninitialised prevRecord!");


                prevRecord.next = nextEmptyOverflowIndex++;

                return FileMenager.GetOverflowFileName();
            }
            else
            {
                page.ReplaceFirstEmpty(toBeInserted);
                return FileMenager.GetPrimaryFileName();
            }
        }

        private void InsertToOverflowFile(Record toBeInserted)
        {
            bool inserted = false;
            for (int i = 0; i < nrOfPageInOverflow && !inserted; i++) 
            {
                Page page = ReadPage(FileMenager.GetOverflowFileName(), i* B);

                if (!page.IsFull())
                {
                    page.ReplaceFirstEmpty(toBeInserted);
                    inserted = true;
                }
            }
            if(!inserted) throw new InvalidOperationException("Enable to insert record to overflow area!");

            //if (IsReorganisation()) Reorganise();

        }

        // przebuduj! - error - zły return i pętla nieskonczona
        private int GetPageNr(int key)
        {
            int position = 0;

            while (true)
            {
                int[,] indexPage = ReadIndexPage(FileMenager.GetIndexFileName(), position);

                int prevKey = 0;

                for (int j = 0; j < indexPage.GetLength(0); j++)
                {
                    int keyFromIndex = indexPage[j, 0];
                    if (key < keyFromIndex) prevKey = key;
                    else if (key > keyFromIndex) return indexPage[prevKey, 1];
                    else return indexPage[key - 1, 1];
                }

                position += B;
            }
        }


        private void ValidRecord(Record record)
        {
            if (record.key <= 0) {
                throw new InvalidOperationException("Invalid key!");
            }
            else if (record.data1 <= 0) {
                throw new InvalidOperationException("Invalid data1!");
            }
            else if (record.data2 <= 0)
            {
                throw new InvalidOperationException("Invalid data2!");
            }
            else if (record.deleted == 1)
            {
                throw new InvalidOperationException("Cannot insert deleted record!");
            }
            else if (record.next != -1)
            {
                throw new InvalidOperationException("Inserted record cannot point to another record!");
            }
        }

        public int FindPageToInsertTo()
        {




            return 0;
        }

    }
}
