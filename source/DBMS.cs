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
        private FileMenager fm;

        public DBMS(FileMenager fm)
        {
            this.fm = fm;
            nextEmptyOverflowIndex = 0;

            InsertSpecialFirstRecordToPrimary();
        }

        private void InsertSpecialFirstRecordToPrimary()
        {
            if (File.Exists(fm.GetPrimaryFileName()))
            {
                Record toBeInserted = new Record(0, 0, 0, 1, -1);
                Page page = ReadPage(fm.GetPrimaryFileName(), 0);
                page.ReplaceFirstEmpty(toBeInserted);
                FileMenager.WriteToFile(fm.GetPrimaryFileName(), page.GetRecords(), 0);
            }
            else
            {
                throw new InvalidOperationException("File menager did not create primary file!");
            }          
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
                            Page page = RecordMenager.BytesToPage(chunk);
                            page.nr = position / B + 1;
                            return page;

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

            int pageNr = GetPageNr(toBeInserted.GetKey());

            Page page = ReadPage(fm.GetPrimaryFileName(), (pageNr - 1) * B);

            Insert(page, toBeInserted, pageNr);
            // page.GetRecords() jest zawsze rowne 1 stronie primary

        }

        private void Insert(Page page, Record toBeInserted, int pageNr)
        {
            Record prevRecord = null;
            
            foreach (var record in page.GetRecords())
            {
                if (record.IsEmpty())
                {
                    
                    page.ReplaceFirstEmpty(toBeInserted);
                    FileMenager.WriteToFile(fm.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
                    return;                  
                }
                else if(record.GetKey() < toBeInserted.GetKey())
                {
                    prevRecord = record;
                }
                else if (record.GetKey() > toBeInserted.GetKey())
                {                   
                    InsertToOverflowFile(toBeInserted, prevRecord, page);

                    //prevRecord = record;
                    //prevRecord.next = nextEmptyOverflowIndex++;
                    //FileMenager.WriteToFile(fm.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
                    return;
                }
                else // ==
                {
                    throw new InvalidOperationException("Cannot insert record! Key duplicated!");
                    /*page.Update(toBeInserted);
                    FileMenager.WriteToFile(fm.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
                    return;*/
                }

            }
            // if not added to overflow nor updated - append

            if (page.IsFull())
            {
                if (prevRecord == null) throw new InvalidProgramException("Uninitialised prevRecord!");

                InsertToOverflowFile(toBeInserted, prevRecord, page);    

                return;
            }
            else
            {
                page.ReplaceFirstEmpty(toBeInserted);
                FileMenager.WriteToFile(fm.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
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
                Page page = ReadPage(fm.GetOverflowFileName(), pageNr * B);

                if (!page.IsFull())
                {
                    page.ReplaceFirstEmpty(toBeInserted);
                    FileMenager.WriteToFile(fm.GetOverflowFileName(), page.GetRecords(), pageNr * B);
                    inserted = true;
                }
            }

            // set correctly pointer !!!!!!!!!!!

            if (!inserted)
            {
                throw new InvalidOperationException("Enable to insert record to overflow area!");
            }

        }
        private void InsertToOverflowFile(Record toBeInserted, Record prevRecord, Page page)
        {

            if (!prevRecord.HasNext())
            {
                InsertToOverflowFileAtEnd(toBeInserted);

                prevRecord.SetNext(nextEmptyOverflowIndex++);
                FileMenager.WriteToFile(fm.GetPrimaryFileName(), page.GetRecords(), (page.nr - 1) * B);
            }
            else
            {
                //Record firstRecordInOverflow = GetRecord(prevRecord.next);
                int overflowPageNr = GetOverflowPageNr(prevRecord.GetNext());
                Page overflowPage = ReadPage(fm.GetOverflowFileName(), (overflowPageNr -1) * B);

                Record firstRecordInOverflow = overflowPage.GetRecords().ElementAt(GetOverflowRecIndexInPage(prevRecord.GetNext()));

                if (firstRecordInOverflow.GetKey() < toBeInserted.GetKey())
                {
                    // ustaw wsk i wpisz to overflow - jesli w overflow skonczylo sie miejsce
                    InsertToOverflowFileAtEnd(toBeInserted);

                    firstRecordInOverflow.SetNext(nextEmptyOverflowIndex++);
                    firstRecordInOverflow.WriteRecToFile(fm.GetOverflowFileName(), prevRecord.GetNext());
                }
                else if (firstRecordInOverflow.GetKey() == toBeInserted.GetKey())
                {
                    throw new InvalidOperationException("Key of inserted record duplicated! Insert rejected!");
                }
                else
                {
                    // swap
                    Record toBeSwapped = new Record(firstRecordInOverflow);
                    firstRecordInOverflow.Update(toBeInserted);

                    //overflowPage.Update(firstRecordInOverflow.key, toBeInserted);

                    firstRecordInOverflow.SetNext(nextEmptyOverflowIndex++);
                    firstRecordInOverflow.WriteRecToFile(fm.GetOverflowFileName(), prevRecord.GetNext());

                    //FileMenager.WriteToFile(fm.GetOverflowFileName(), overflowPage.GetRecords(), (overflowPage.nr - 1) * B);

                    InsertToOverflowFileAtEnd(toBeSwapped);
                    

                }

            }

            // error - what if inserted record to overflow should be pointed by another record from overflow

            

            //if (IsReorganisation()) Reorganise();

        }

        private int GetOverflowRecIndexInPage(int globalIndex)
        {
            return globalIndex % recPerPage;
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

            if (record.GetKey() <= 0) {
                throw new InvalidOperationException("Invalid key!");
            }
            else if (record.GetData1() <= 0) {
                throw new InvalidOperationException("Invalid data1!");
            }
            else if (record.GetData2() <= 0)
            {
                throw new InvalidOperationException("Invalid data2!");
            }
            else if (record.GetDeleted() == 1)
            {
                throw new InvalidOperationException("Cannot insert deleted record!");
            }
            else if (record.GetNext() != -1)
            {
                throw new InvalidOperationException("Inserted record cannot point to another record!");
            }
        }

        public void DisplayIndexContent()
        {
            
        }

        public void DisplayDBAscending()
        {
            for (int pageNr = 0; pageNr < nrOfPageInPrimary; pageNr++)
            {
                Page page = ReadPage(fm.GetPrimaryFileName(), pageNr * B);

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
                        Console.WriteLine(record.ToString());
                        if(record.GetNext() != -1)
                        {
                            // wef know that record points to next record
                            DisplayOverflowChain(record);
                            
                        }
                    }
                }
            }

        }

        private void DisplayOverflowChain(Record anchor)
        {
            // could do better - always opening page instead of search through whole page
            bool endOfChain = false;
            while (!endOfChain)
            {
                if (anchor.HasNext())
                {
                    anchor = GetNextRecordFromOverflow(anchor);
                    Console.WriteLine(anchor.ToString());
                }
                else
                {
                    endOfChain = true;
                }
            }
        }

        //
        // create function that cleans chains pointers !!!!
        //
        private List<Record> GetOverflowChain(int anchorKey)
        {
            var chain = new List<Record>();
            Record anchor = GetRecord(anchorKey);
            bool endOfChain = false;
            while (!endOfChain)
            {
                if (anchor.HasNext())
                {
                    anchor = GetNextRecordFromOverflow(anchor);
                    if (anchor.GetDeleted() == 0)
                    {
                        chain.Add(anchor);
                    }
                }
                else
                {
                    endOfChain = true;
                }

            }
            return chain;
        }

        public Record GetRecord(int keyOfRecToFind)
        {
            int pageNr = GetPageNr(keyOfRecToFind);

            Page page = ReadPage(fm.GetPrimaryFileName(), (pageNr - 1) * B);

            Record prevRecord = null;

            foreach (var record in page.GetRecords())
            {
                if (record.IsEmpty() && record.GetDeleted() == 0)
                {
                    return null;
                }
                else if (record.GetKey() < keyOfRecToFind)
                {
                    prevRecord = record;
                }
                else if (record.GetKey() == keyOfRecToFind)
                {
                    return record;
                }
                else //if (record.key > key)
                {
                    return GetRecordFromOverflow(prevRecord, keyOfRecToFind);
                }

            }
            throw new InvalidOperationException("Pointer to empty record - record does not exist???!");
        }
        public Record GetNextRecordFromOverflow(Record prevRecord)
        {
            if (prevRecord == null) throw new InvalidOperationException("uninitialised!");
            bool isFound = false;
            Record toFound = null;
            while (!isFound)
            {
                int indexInOverflow = prevRecord.GetNext();

                int overflowPageNr = GetOverflowPageNr(indexInOverflow);
                int overflowPageNrSecondAttepmt = -1;
                Page overflowPage = null;
                if (overflowPageNr != overflowPageNrSecondAttepmt)
                {
                    overflowPage = ReadPage(fm.GetOverflowFileName(), (overflowPageNr - 1) * B);
                }

                int indexInPage = indexInOverflow % recPerPage;
                toFound = overflowPage.GetRecords().ElementAt(indexInPage);

                if (toFound.IsEmpty())
                {
                    throw new InvalidOperationException("Pointer to empty record - record does not exist???!");
                }
                else
                {
                    isFound = true;
                }

            }
            return toFound;

        }
        public Record GetRecordFromOverflow(Record prevRecord, int keyOfRecToFind)
        {
            if (prevRecord == null) throw new InvalidOperationException("uninitialised!");
            bool isFound = false;
            Record toFound = null;
            while (!isFound)
            {
                int indexInOverflow = prevRecord.GetNext();
                
                int overflowPageNr = GetOverflowPageNr(indexInOverflow);
                int overflowPageNrSecondAttepmt = -1;
                Page overflowPage = null;
                if (overflowPageNr != overflowPageNrSecondAttepmt)
                {
                    overflowPage = ReadPage(fm.GetOverflowFileName(), (overflowPageNr - 1) * B);
                }

                int indexInPage = indexInOverflow % recPerPage;
                toFound = overflowPage.GetRecords().ElementAt(indexInPage);

                if (!toFound.IsEmpty())
                {
                    if (toFound.GetKey() == keyOfRecToFind)
                    {
                        isFound = true;
                    }
                    else if (toFound.GetNext() != -1) 
                    {
                        // we have to dive deeeeper..

                        // check if requested record is in the downoladed page
                        overflowPageNrSecondAttepmt = GetOverflowPageNr(toFound.GetNext());
                        prevRecord = toFound;                      
                    }
                    else throw new InvalidOperationException("Pointer to empty record - record does not exist???!");
                }
                else
                {
                    throw new InvalidOperationException("Pointer to empty record - record does not exist???!");
                }
            }
            return toFound;

        }

        private int GetOverflowPageNr(int indexInOverflow)
        {
            return (int)Math.Ceiling((indexInOverflow + 1.0f) / (double)recPerPage);
        }

        public int GetNrOfPagesOfFile(string filePath)
        {
            if (filePath == fm.GetPrimaryFileName())
                return nrOfPageInPrimary;
            else if (filePath == fm.GetOverflowFileName())
                return nrOfPageInOverflow;
            else
                throw new InvalidOperationException("Cannot get nr of pages!");
        }

        internal string ReadRecord(int key)
        {

            Record foundedrecord = GetRecord(key);
            return (foundedrecord != null) ? foundedrecord.ToString() : "Record doesn't exist!";

        }

        public void DisplayFileContent(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            Console.WriteLine($"###### {fileName} ######");
            for (int position = 0; position < GetNrOfPagesOfFile(filePath); position++)
            {
                Console.WriteLine($"------ Page: {position + 1} ------");
                Page page = DBMS.ReadPage(filePath, position * DBMS.B);
                foreach (var record in page.GetRecords())
                {
                    Console.Write(record.ToString());
                }
            }
        }

        public void DeleteRecord(int key)
        {
            Record toDelete = GetRecord(key);
            if(toDelete == null)
            {
                throw new InvalidOperationException("Record to delete does not exist!");
            }
            else
            {
                toDelete.Delete();
            }
        }

    }
}
