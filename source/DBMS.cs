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
        public const int R = 20;   // size of record - 5* int 
        private const int K = 4;   // size of key - int 
        private const int P = 4;   // size of pointer - int 
        private const double alpha = 0.5;   // page utlilization factor in the main area just after reorganization, α < 1
        public const int defaultNrOfPages = 3;
        public const int nrOfIntsInRecord = R / 4;
        public const int recPerPage = B / R;
        public const double delta = 0.2;  // fullfillment of overflow ratio to fullfillment of primary 


        // change after reorganisation
        public static int nrOfPageInPrimary = defaultNrOfPages;
        public static int nrOfPageInOverflow = defaultNrOfPages;

        private int nextEmptyOverflowIndex;

        public DBMS()
        {
            nextEmptyOverflowIndex = 0;

            if (File.Exists(FileMenager.GetPrimaryFileName()))
            {
                InsertSpecialFirstRecordToPrimary();
            }
            else
            {
                throw new InvalidOperationException("File menager did not create primary file!");
            }
        }

        private void InsertSpecialFirstRecordToPrimary()
        {
            Record toBeInserted = new Record(0, 0, 0, 1, -1);
            Page page = ReadPage(FileMenager.GetPrimaryFileName(), 0);
            page.ReplaceFirstEmpty(toBeInserted);
            FileMenager.WriteToFile(FileMenager.GetPrimaryFileName(), page.GetRecords(), 0);
        }

        public static Page ReadPage(string filePath, int position)
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

            ChoosePlaceAndInsert(page, toBeInserted, pageNr);
            // page.GetRecords() jest zawsze rowne 1 stronie primary
            

        }

        private void ChoosePlaceAndInsert(Page page, Record toBeInserted, int pageNr)
        {
            Record prevRecord = null;
            
            foreach (var record in page.GetRecords())
            {
                if (record.IsEmpty())
                {
                    
                    page.ReplaceFirstEmpty(toBeInserted);
                    FileMenager.WriteToFile(FileMenager.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
                    return;                  
                }
                else if(record.key < toBeInserted.key)
                {
                    prevRecord = record;
                }
                else if (record.key > toBeInserted.key)
                {
                    
                    InsertToOverflowFile(toBeInserted, prevRecord);

                    //prevRecord = record;
                    prevRecord.next = nextEmptyOverflowIndex++;
                    FileMenager.WriteToFile(FileMenager.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
                    return;
                }
                else // ==
                {
                    throw new InvalidOperationException("Cannot insert record! Key duplicated!");
                    /*page.Update(toBeInserted);
                    FileMenager.WriteToFile(FileMenager.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
                    return;*/
                }

            }
            // if not added to overflow nor updated - append

            if (page.IsFull())
            {
                if (prevRecord == null) throw new InvalidProgramException("Uninitialised prevRecord!");

                InsertToOverflowFile(toBeInserted, prevRecord);    

                prevRecord.next = nextEmptyOverflowIndex++;
                FileMenager.WriteToFile(FileMenager.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);

                return;
            }
            else
            {
                page.ReplaceFirstEmpty(toBeInserted);
                FileMenager.WriteToFile(FileMenager.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
                return;
            }
            // if function return earlier - record was inserted to overflow file,
            // otherwise record will be inserted to primary

            
        }
        private void InsertToOverflowFileAtEnd(Record toBeInserted)
        {
            bool inserted = false;

            for (int pageNr = 0; pageNr < nrOfPageInOverflow && !inserted; pageNr++)
            {
                Page page = ReadPage(FileMenager.GetOverflowFileName(), pageNr * B);

                if (!page.IsFull())
                {
                    page.ReplaceFirstEmpty(toBeInserted);
                    FileMenager.WriteToFile(FileMenager.GetOverflowFileName(), page.GetRecords(), pageNr * B);
                    inserted = true;
                }
            }

            // set correctly pointer !!!!!!!!!!!

            if (!inserted)
            {
                throw new InvalidOperationException("Enable to insert record to overflow area!");
            }

        }
        private void InsertToOverflowFile(Record toBeInserted, Record prevRecord)
        {

            if (!prevRecord.HasNext())
            {
                InsertToOverflowFileAtEnd(toBeInserted);
            }
            else
            {
                Record nextRecordInOverflow = GetRecord(prevRecord.next);

                if(nextRecordInOverflow.key < toBeInserted.key)
                {
                    // ustaw wsk i wpisz to overflow - jesli w overflow skonczylo sie miejsce
                    InsertToOverflowFileAtEnd(toBeInserted);

                    nextRecordInOverflow.next = nextEmptyOverflowIndex++;
                    nextRecordInOverflow.WriteToFile(FileMenager.GetOverflowFileName(), prevRecord.next);
                }
                else
                {
                    // swap



                }


            }

            // error - what if inserted record to overflow should be pointed by another record from overflow

            

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


        public void DisplayDBContent()
        {
            for (int pageNr = 0; pageNr < nrOfPageInPrimary; pageNr++)
            {
                Page page = ReadPage(FileMenager.GetPrimaryFileName(), pageNr * B);

                foreach (var record in page.GetRecords())
                {
                    if (record.IsEmpty())
                    {
                        // we know that first record is empty so we know that page is empty,
                        // but next pages may contain records
                        break;
                    }
                    else
                    {
                        record.ToString();
                        if(record.next != -1)
                        {
                            // we know that record points to next reocrd

                        }
                    }
                }
            }

        }

        public Record GetRecord(int keyOfRecToFound)
        {
            int pageNr = GetPageNr(keyOfRecToFound);

            Page page = ReadPage(FileMenager.GetPrimaryFileName(), (pageNr - 1) * B);

            Record prevRecord = null;

            foreach (var record in page.GetRecords())
            {
                if (record.IsEmpty() && record.deleted == 0)
                {
                    // record does not exist
                }
                else if (record.key < keyOfRecToFound)
                {
                    prevRecord = record;
                }
                else if (record.key == keyOfRecToFound)
                {
                    return record;
                }
                else //if (record.key > key)
                {
                    // szukamy w overflow pod indeksem record.next
                    bool isFound = false;
                    Record toFound = null;
                    while (!isFound)
                    {                      
                        int indexInOverflow = prevRecord.next;
                        if (prevRecord == null) throw new InvalidOperationException("uninitialised!");

                        int overflowPageNr = GetOverflowPageNr(indexInOverflow);
                        int overflowPageNrSecondAttepmt = -1;
                        Page overflowPage = null;
                        if (overflowPageNr != overflowPageNrSecondAttepmt)
                        {
                            overflowPage = ReadPage(FileMenager.GetOverflowFileName(), (overflowPageNr - 1) * B);
                        }

                        int indexInPage = indexInOverflow % recPerPage;
                        toFound = overflowPage.GetRecords().ElementAt(indexInPage);

                        if (!toFound.IsEmpty())
                        {
                            if (toFound.key == keyOfRecToFound)
                            {
                                isFound = true;
                            }
                            else if (toFound.next != -1)
                            {
                                // we have to dive deeeeper..

                                // check if requested record is in the downoladed page
                                overflowPageNrSecondAttepmt = GetOverflowPageNr(toFound.next);
                                //if (overflowPageNr == overflowPageNrSecondAttepmt)
                                prevRecord = toFound;
                            }
                            else
                            {
                                throw new InvalidOperationException("Pointer to empty record - record does not exist???!");
                            }


                        }
                        else
                        {
                            throw new InvalidOperationException("Pointer to empty record - record does not exist???!");
                        }
                    }
                    return toFound;
                }

            }
            throw new InvalidOperationException("Pointer to empty record - record does not exist???!");
        }

        private int GetOverflowPageNr(int indexInOverflow)
        {
            return (int)Math.Ceiling((indexInOverflow + 1.0f) / (double)recPerPage);
        }

        public static int GetNrOfPagesOfFile(string filePath)
        {
            if (filePath == FileMenager.GetPrimaryFileName())
                return nrOfPageInPrimary;
            else if (filePath == FileMenager.GetOverflowFileName())
                return nrOfPageInOverflow;
            else
                throw new InvalidOperationException("Cannot get nr of pages!");
        }

    }
}
