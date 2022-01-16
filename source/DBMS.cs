using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ISFO.source
{
    class DBMS
    {
        public const int B = 80;  // disk page capacity - nr of bytes readed at once - always must be multiple of 'R'
        public const int R = 20;   // size of record - 5* int 
        public const int K = 4;   // size of key - int 
        public const int P = 4;   // size of pointer - int 
        public double alpha = 0.5;   // page utlilization factor in the main area just after reorganization, α < 1
        public const int defaultNrOfPages = 1;
        public const int nrOfIntsInRecord = R / 4;
        public double delta = 0.25;  // fullfillment of overflow   
        public const double sizeCoeff = 0.2;
        public static int bf = (int)Math.Floor((double)(B / R));    // attention! - Record includes 'P' !
        public static int bi = (int)Math.Floor((double)(B / (K + P)));  // = 8 

        // change after reorganisation
        public static int nrOfPagesInPrimary = defaultNrOfPages;
        public static int nrOfPagesInOverflow =  (int)Math.Ceiling(defaultNrOfPages * sizeCoeff);
        public static int nrOfPagesInIndex = (int)Math.Ceiling(nrOfPagesInPrimary / (double)bi);
        public static int nrOfPagesInIndexOld = nrOfPagesInIndex;
        public static int V = 0;
        public static int N = 0;

        // stats
        public static int nrOfOperations = 0;
        public static int fileSize = 0;

        public bool isDebug;

        private int nextEmptyOverflowIndex;
        private FileMenager fm;

        public DBMS(FileMenager fm, bool isDebug = true)
        {
            SetParametersDynamically(alpha, delta);

            this.isDebug = isDebug;
            this.fm = fm;
            nextEmptyOverflowIndex = 0;

            InsertSpecialFirstRecord(fm.GetPrimaryFileName());
            N++;
        }

        public void SetParametersDynamically(double alpha, double delta)
        {
            this.alpha = alpha;
            this.delta = delta;
        }

        private void InsertSpecialFirstRecord(string filePath)
        {
            if (File.Exists(filePath))
            {
                Record toBeInserted = new Record(0, 0, 0, 1, -1);
                FileMenager.WriteToFile(filePath, new[] { toBeInserted }, 0);

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
                            page.SetPageNr(position / B + 1);
                            page.SetPageFilePath(filePath);

                            nrOfOperations++;
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
            if (IsRecordValid(toBeInserted))
            {
                int pageNr = GetPageNr(toBeInserted.GetKey());

                Page page = ReadPage(fm.GetPrimaryFileName(), (pageNr - 1) * B);

                Insert(page, toBeInserted, pageNr);
            }
            else
            {
                Console.WriteLine("Insert command is invalid!");
            }
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
                    N++;
                    return;                  
                }
                else if(record.GetKey() < toBeInserted.GetKey())
                {
                    prevRecord = record;
                }
                else if (record.GetKey() > toBeInserted.GetKey())
                {                   
                    InsertToOverflowFile(toBeInserted, prevRecord, page);
                    return;
                }
                else // ==
                {
                    Console.WriteLine("Cannot insert or update record! Key duplicated!");
                    return;
                }

            }
            // if not added to overflow nor updated - append

            if (page.IsFull())
            {
                if (prevRecord == null) throw new InvalidProgramException("Uninitialised prevRecord!");

                InsertToOverflowFile(toBeInserted, prevRecord, page);    
            }
            else
            {
                page.ReplaceFirstEmpty(toBeInserted);
                FileMenager.WriteToFile(fm.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
                N++;
            }
            return;
            // if function return earlier - record was inserted to overflow file,
            // otherwise record will be inserted to primary

            
        }
        private void InsertToOverflowFileAtEnd(Record toBeInserted)
        {
            bool inserted = false;

            for (int pageNr = 0; pageNr < nrOfPagesInOverflow && !inserted; pageNr++)
            {
                Page page = ReadPage(fm.GetOverflowFileName(), pageNr * B);

                if (!page.IsFull())
                {
                    page.ReplaceFirstEmpty(toBeInserted);
                    FileMenager.WriteToFile(fm.GetOverflowFileName(), page.GetRecords(), pageNr * B);
                    inserted = true;
                    V++;

                    // check if all pages are full (mayby with deleted records..)
                    if(page.IsFull() && pageNr + 1 == nrOfPagesInOverflow) Reorganise();
                }
            }

            if (!inserted)
            {
                throw new InvalidOperationException("Enable to insert record to overflow area!");
            }

        }
        private void InsertToOverflowFile(Record toBeInserted, Record prevRecord, Page page)
        {

            if (!prevRecord.HasNext())
            {
                // set pointer to next
                prevRecord.SetNext(nextEmptyOverflowIndex++);
                FileMenager.WriteToFile(fm.GetPrimaryFileName(), page.GetRecords(), (page.nr - 1) * B);

                InsertToOverflowFileAtEnd(toBeInserted);
            }
            else
            {
                int overflowPageNr = GetOverflowPageNr(prevRecord.GetNext());
                Page overflowPage = ReadPage(fm.GetOverflowFileName(), (overflowPageNr -1) * B);

                Record firstRecordInOverflow = overflowPage.GetRecords().ElementAt(GetOverflowRecIndexInPage(prevRecord.GetNext()));

                if (firstRecordInOverflow.GetKey() < toBeInserted.GetKey())
                {
                    // ustaw wsk i wpisz to overflow - jesli w overflow skonczylo sie miejsce
                    firstRecordInOverflow.SetNext(nextEmptyOverflowIndex++);
                    firstRecordInOverflow.WriteRecToFile(fm.GetOverflowFileName(), prevRecord.GetNext());

                    InsertToOverflowFileAtEnd(toBeInserted);

                    
 
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
                    firstRecordInOverflow.SetNext(nextEmptyOverflowIndex++);
                    firstRecordInOverflow.WriteRecToFile(fm.GetOverflowFileName(), prevRecord.GetNext());
                    // czemu ma wsk ???
                    InsertToOverflowFileAtEnd(toBeSwapped);
                }

            }

            if (IsReorganisation()) Reorganise();
        }
        private void Update(int keyOfRecToUpdate, Record freshRecord)
        {
            if(!IsRecordValid(freshRecord))
            {
                Console.WriteLine("Invalid record!");
                return;
            }

            Page page = GetPage(keyOfRecToUpdate);

            if (page != null)
            {
                Record toBeUpdated = page.Get(keyOfRecToUpdate);
                if (toBeUpdated.Exist())
                {
                    if (keyOfRecToUpdate == freshRecord.GetKey())
                    {
                        toBeUpdated.Update(freshRecord);
                        FileMenager.WriteToFile(page.pageFilePath, page.GetRecords(), (page.nr - 1) * B);
                    }
                    else
                    {
                        // nice patch :) 
                        Record potentialyYetInDB = GetRecord(freshRecord.GetKey());
                        if (potentialyYetInDB == null)
                        {
                            toBeUpdated.Delete();
                            FileMenager.WriteToFile(page.pageFilePath, page.GetRecords(), (page.nr - 1) * B);
                            DecrementRecordCnt(page.pageFilePath);

                            InsertRecord(freshRecord);
                        }
                        else
                        {
                            Console.WriteLine("Cannot update record! New key is duplicated!");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Record to update doesn't exist!");
                }
            }
            else
            {
                Console.WriteLine("Record to update doesn't exist!");
            }
        }
        private void DecrementRecordCnt(string pageFilePath)
        {
            if(pageFilePath == fm.GetPrimaryFileName())
            {
                N--;
            }
            else if(pageFilePath == fm.GetOverflowFileName())
            {
                V--;
            }
        }
        private Page GetPage(int keyOfRecToFind)
        {
            // returns null if key was not found
            int pageNr = GetPageNr(keyOfRecToFind);

            Page page = ReadPage(fm.GetPrimaryFileName(), (pageNr - 1) * B);

            Record recToFind = page.Get(keyOfRecToFind);
            if(recToFind != null)
            {
                return page;
            }
            else
            {   // we have to check if record is in overflow
                Record prevRecord = null;

                foreach (var record in page.GetRecords())
                {
                    if (record.IsEmpty())
                    {
                        return null;
                    }
                    else if (record.GetKey() < keyOfRecToFind)
                    {
                        prevRecord = record;
                    }
                    else if (record.GetKey() > keyOfRecToFind)
                    {

                        return GetPageFromOverflow(prevRecord, keyOfRecToFind);
                    }
                }
            }
            return null;
        }
        private Page GetPageFromOverflow(Record prevRecord, int keyOfRecToFind)
        {
            if (prevRecord == null) throw new InvalidOperationException("uninitialised!");

            bool isFound = false;
            Page overflowPage = null;
            int overflowPageNr;
            bool isNextRecordOnReadPage = false;
            while (!isFound)
            {
                int indexInOverflow = prevRecord.GetNext();
                if (indexInOverflow == -1) break;

                if (!isNextRecordOnReadPage)
                {
                    overflowPageNr = GetOverflowPageNr(indexInOverflow);
                    overflowPage = ReadPage(fm.GetOverflowFileName(), (overflowPageNr - 1) * B);
                }

                int indexInPage = indexInOverflow % bf;

                Record toFound = overflowPage.GetRecords().ElementAt(indexInPage);

                if (!toFound.IsEmpty())
                {
                    if (toFound.GetKey() == keyOfRecToFind)
                    {
                        isFound = true;
                    }
                    else if (toFound.GetNext() != -1)
                    {
                        // check if requested record is in the downoladed page
                        isNextRecordOnReadPage = IsNextRecordOnReadPage(prevRecord.GetNext(), toFound.GetNext());
                        //overflowPageNrSecondAttempt = GetOverflowPageNr(toFound.GetNext());
                        prevRecord = toFound;
                    }
                    else
                    {                       
                        Console.WriteLine("Pointer to empty record - record does not exist!");
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine("Pointer to empty record - record does not exist!");
                    return null;
                }
            }
            return overflowPage;

        }
        private bool IsNextRecordOnReadPage(int anchorIndex, int nextRecIndex)
        {
            int pageNr = anchorIndex / bf;

            if (nextRecIndex >= anchorIndex && nextRecIndex < pageNr * (bf + 1))
                return true;
            else
                return false;

        }
        private void Delete(int keyOfRecToBeDeleted)
        {
            Page page = GetPage(keyOfRecToBeDeleted);
            if(page != null)
            {
                Record toDelete = page.Get(keyOfRecToBeDeleted);
                if (toDelete.GetDeleted() != 1)
                {
                    toDelete.Delete();
                    FileMenager.WriteToFile(page.pageFilePath, page.GetRecords(), (page.nr - 1) * B);
                    DecrementRecordCnt(page.pageFilePath);
                }
                else
                {
                    Console.WriteLine("Record to delete has been deleted yet!");
                }
            }
            else
            {
                Console.WriteLine("Record to delete doesn't exist!");
            }
        }
        public void Reorganise()
        {

            Console.WriteLine($"\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" +
                $"~~~~~~ Reorganisation ~~~~~~\n" +
                $"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");

            if (isDebug)
            {
                DisplayIndexFileContent(fm.GetIndexFileName());
                DisplayFileContent(fm.GetPrimaryFileName());
                DisplayFileContent(fm.GetOverflowFileName());

                Console.WriteLine($"N: {N}");
                Console.WriteLine($"V: {V}");
            }

            int nrOfPagesInOldPrimary = nrOfPagesInPrimary;
            GenerateFilesToReorganisation();
            InsertSpecialFirstRecord(fm.GetPrimaryNewFileName());

            int pageNrInOldPrimary = 0;
            int pageNrInNewPrimary = 0;
            int pageNrInNewIndex = 0;
            int pageNrInIndex = 1;

            var newIndexPage = new List<(int key, int pageNr)>();
            const int firstKeyInIndex = 1;
            newIndexPage.Add((firstKeyInIndex, pageNrInIndex++));

            Page oldPrimaryPage;
            Page newPrimaryPage = ReadPage(fm.GetPrimaryNewFileName(), pageNrInNewPrimary * B);

            bool indexPageInserted = false;

            while (pageNrInOldPrimary< nrOfPagesInOldPrimary && pageNrInNewPrimary < nrOfPagesInPrimary)
            {
                indexPageInserted = false;

                oldPrimaryPage = ReadPage(fm.GetPrimaryFileName(), pageNrInOldPrimary * B);

                foreach (var oldRecord in oldPrimaryPage.GetRecords())
                {
                    if (oldRecord.IsEmpty()) break;
                    foreach (var item in GetOverflowChain(oldRecord.GetKey()))
                    {
                        if (item.Exist())
                        {
                            if (newPrimaryPage.MyGetLength() >= (int)Math.Ceiling(alpha * bf))
                            {   
                                newIndexPage.Add((key: item.GetKey(), pageNr: pageNrInIndex++));
                                if (newIndexPage.Count() == bi)
                                {
                                    FileMenager.WriteToIndexFile(fm.GetIndexNewFileName(), newIndexPage, pageNrInNewIndex * B);
                                    pageNrInNewIndex++;
                                    newIndexPage.Clear();
                                    indexPageInserted = true;
                                }

                                // we save fullfilled page 
                                FileMenager.WriteToFile(fm.GetPrimaryNewFileName(), newPrimaryPage.GetRecords(), pageNrInNewPrimary * B);
                                pageNrInNewPrimary++;
                                newPrimaryPage = ReadPage(fm.GetPrimaryNewFileName(), pageNrInNewPrimary * B);
                            }
                            item.SetNext(-1);
                            newPrimaryPage.ReplaceFirstEmpty(item);
                        }
                    }
                }
                pageNrInOldPrimary++;
            }

            // always save not full pages
            if (newPrimaryPage != null)
            {   
                if (!newPrimaryPage.IsEmpty())
                {
                    FileMenager.WriteToFile(fm.GetPrimaryNewFileName(), newPrimaryPage.GetRecords(), pageNrInNewPrimary * B);
                }
            }

            if (!indexPageInserted)
            {
                FileMenager.WriteToIndexFile(fm.GetIndexNewFileName(), newIndexPage, pageNrInNewIndex * B);
            }

            // menage files and set counters
            DeleteOldFiles();
            RenameFilesAfterReorganisation();

            N += V;
            V = 0;
            
            nextEmptyOverflowIndex = 0;
            nrOfPagesInIndexOld = nrOfPagesInIndex;
        }
        private void DeleteOldFiles()
        {
            File.Delete(fm.GetIndexFileName());
            File.Delete(fm.GetPrimaryFileName());
            File.Delete(fm.GetOverflowFileName());
        }
        public void GenerateFilesToReorganisation()
        {
            nrOfPagesInPrimary = (int)Math.Ceiling(((double)N + V) / (double)(bf * alpha));
            nrOfPagesInIndexOld = nrOfPagesInIndex;
            nrOfPagesInIndex = (int)Math.Ceiling(nrOfPagesInPrimary / (double)bi);
            nrOfPagesInOverflow = (int)Math.Ceiling(nrOfPagesInPrimary * sizeCoeff);

            FileMenager.GenerateIndexFile(fm.GetIndexNewFileName() /*, nrOfPagesInIndex*/);
            FileMenager.GenerateAreaFile(fm.GetPrimaryNewFileName(), nrOfPagesInPrimary);
            FileMenager.GenerateAreaFile(fm.GetOverflowNewFileName(), nrOfPagesInOverflow);
        }
        private void RenameFilesAfterReorganisation()
        {

            File.Move(fm.GetIndexNewFileName(), fm.GetIndexFileName());
            File.Move(fm.GetPrimaryNewFileName(), fm.GetPrimaryFileName());
            File.Move(fm.GetOverflowNewFileName(), fm.GetOverflowFileName());


        }
        private bool IsReorganisation()
        {   
            return V / (double)N >= delta;           
        }
        private int GetOverflowRecIndexInPage(int globalIndex)
        {
            return globalIndex % bf;
        }

        // przebuduj! - error - zły return i pętla nieskonczona
        private int GetPageNr(int key)
        {
            int pageIndex = 0;

            int pageNr = 0;

            int[,] indexPage = null;
            while (pageIndex < nrOfPagesInIndexOld)    // bylo nrOfPagesInIndex
            {
                indexPage = ReadIndexPage(fm.GetIndexFileName(), pageIndex * B);

                for (int i = 0; i < indexPage.GetLength(0); i++)
                {
                    int indexKey = indexPage[i, 0];
                    if (key == 0)
                    {   // we handle record with key = 0
                        return 1;
                    }
                    if(indexKey > key)  
                    {
                        return pageNr;
                    }

                    pageNr++;
                }
                pageIndex++;
            }
            return pageNr;
            //int lastIndex = indexPage.GetLength(0) - 1;
            //int lastKeyInIndex = indexPage[lastIndex, 0];
            //if (lastKeyInIndex <= key)
            //{   
            //    return indexPage[lastIndex, 1];
            //}
            //else
            //{
            //    throw new InvalidOperationException("Cannot get page nr!");
            //}
        }

        private bool IsRecordValid(Record record)
        {
            if (record.GetKey() <= 0 || record.GetData1() <= 0 || record.GetData2() <= 0 ||
                record.GetDeleted() == 1 || record.GetNext() != -1) {
                return false;
            }
            
            return true;
        }
        public void DisplayDBAscending()
        {
            for (int pageNr = 0; pageNr < nrOfPagesInPrimary; pageNr++)
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
        public void DisplayFileContent(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            if (filePath == fm.GetIndexFileName())
            {
                DisplayIndexFileContent(filePath);
            }
            else
            {
                Console.WriteLine($"###### {fileName} ######");
                for (int position = 0; position < GetNrOfPagesOfFile(filePath); position++)
                {
                    Console.WriteLine($"------ Page: {position + 1} ------");
                    Page page = ReadPage(filePath, position * B);
                    foreach (var record in page.GetRecords())
                    {
                        if(record.GetKey() != 0)
                            Console.Write(record.ToString());
                        else if (record.GetDeleted() == 1)
                        {
                            Console.Write(record.ToString());
                        }
                    }
                }
                Console.WriteLine();
            }
        }

        // to change - function display should display every file
        public void DisplayIndexFileContent(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            Console.WriteLine($"###### {fileName} ######");
            for (int position = 0; position < GetNrOfPagesOfFile(filePath); position++)
            {
                Console.WriteLine($"------ Page: {position + 1} ------");
                var page = ReadIndexPage(filePath, position * B);
                for (int i = 0; i < page.GetLength(0); i++)
                {
                    
                    Console.WriteLine($"{page[i, 0]} {page[i, 1]}");
                }
            }
            Console.WriteLine();
        }

        //
        // create function that cleans chains pointers !!!!
        //
        private List<Record> GetOverflowChain(int anchorKey)
        {
            var chain = new List<Record>();
            Record anchor = GetRecord(anchorKey);

            // with pre..pre anchor
            chain.Add(anchor);


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
            // tu jest problem? 
            foreach (var record in page.GetRecords())
            {
                if (record.IsEmpty() && record.GetDeleted() == 0)
                {   // niepotrzebne?
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
            return null;
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
                int overflowPageNrSecondAttempt = -1;
                Page overflowPage = null;
                if (overflowPageNr != overflowPageNrSecondAttempt)
                {
                    overflowPage = ReadPage(fm.GetOverflowFileName(), (overflowPageNr - 1) * B);
                }

                int indexInPage = indexInOverflow % bf;
                toFound = overflowPage.GetRecords().ElementAt(indexInPage);

                if (toFound.IsEmpty())
                {
                    //return null;
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
            int overflowPageNrSecondAttempt = -1;
            while (!isFound)
            {
                int indexInOverflow = prevRecord.GetNext();
                if (indexInOverflow == -1) break;
                int overflowPageNr = GetOverflowPageNr(indexInOverflow);
                
                Page overflowPage = null;
                if (overflowPageNr != overflowPageNrSecondAttempt)
                {
                    overflowPage = ReadPage(fm.GetOverflowFileName(), (overflowPageNr - 1) * B);
                }

                int indexInPage = indexInOverflow % bf;
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
                        overflowPageNrSecondAttempt = GetOverflowPageNr(toFound.GetNext());
                        prevRecord = toFound;
                    }
                    else return null;
                    //else throw new InvalidOperationException("Pointer to empty record - record does not exist???!");
                }
                else
                {
                    return null;
                    //throw new InvalidOperationException("Pointer to empty record - record does not exist???!");
                }
            }
            return toFound;

        }

        private int GetOverflowPageNr(int indexInOverflow)
        {
            return (int)Math.Ceiling((indexInOverflow + 1.0f) / (double)bf);
        }

        public int GetNrOfPagesOfFile(string filePath)
        {
            if (filePath == fm.GetPrimaryFileName())
                return nrOfPagesInPrimary;
            else if (filePath == fm.GetOverflowFileName())
                return nrOfPagesInOverflow;
            else if (filePath == fm.GetIndexFileName())
                return nrOfPagesInIndex;
            else
                throw new InvalidOperationException("Cannot get nr of pages!");
        }

        public void CmdHandler(string[] cmds)
        {

            foreach (var cmd in cmds)
            {
                Console.WriteLine(
                    $"\n   ******************\n" +
                    $"   * cmd: {cmd} *\n" +
                    $"   ******************\n");
              
                CmdInterpreter(cmd);
                if (isDebug)
                {
                    DisplayIndexFileContent(fm.GetIndexFileName());
                    DisplayFileContent(fm.GetPrimaryFileName());
                    DisplayFileContent(fm.GetOverflowFileName());

                    Console.WriteLine($"N: {N}");
                    Console.WriteLine($"V: {V}");
                }

                //DisplayStats();
            }
        }

        private void DisplayStats()
        {
            // sub operations needed for display!!
            // reset nr of op.!!
            Console.WriteLine(
                $"-------- STATS -------\n" +
                $"operations: {nrOfOperations}\n" +
                $"----------------------\n");
        }

        public void CmdInterpreter(string cmd)
        {
            if (cmd.Contains("I"))
            {
                Regex rx = new Regex(@"^I [0-9]* [0-9]* [0-9]*$");
                if (rx.IsMatch(cmd))
                {
                    List<int> recData = RetriveIntsFromString(cmd);
                    Record record = new Record(recData[0], recData[1], recData[2]);
                    InsertRecord(record);
                }
                else Console.WriteLine("Wrong command!");
            }
            else if (cmd.Contains("U"))
            {
                Regex rx = new Regex(@"^U [0-9]* [0-9]* [0-9]* [0-9]*$");
                if (rx.IsMatch(cmd))
                {
                    List<int> recData = RetriveIntsFromString(cmd);
                    int keyOfRecToUpdate = recData[0];
                    Record freshRecord = new Record(recData[1], recData[2], recData[3]);
                    Update(keyOfRecToUpdate, freshRecord);
                }
                else Console.WriteLine("Wrong command!");
            }
            else if (cmd.Contains("D"))
            {
                Regex rx = new Regex(@"^D [0-9]*$");
                if (rx.IsMatch(cmd))
                {
                    List<int> recData = RetriveIntsFromString(cmd);
                    Delete(recData[0]);
                }
                else Console.WriteLine("Wrong command!");
            }
            else if (cmd == "REORG")
            {
                Reorganise();
            }
            else if (cmd == "DISP")
            {
                DisplayDBAscending();
            }
            else if (cmd == "SHOW")
            {
                Regex rx = new Regex(@"^D [0-9]*$");
                if (rx.IsMatch(cmd))
                {
                    List<int> recData = RetriveIntsFromString(cmd);
                    int keyOfRecToShow = recData[0];
                    ShowRecord(keyOfRecToShow);
                }
                else Console.WriteLine("Wrong command!");
            }
            else Console.WriteLine("Wrong command!");
        }

        private void ShowRecord(int keyOfRecToShow)
        {
            Page page = GetPage(keyOfRecToShow);
            if (page != null)
            {
                Console.WriteLine(page.Get(keyOfRecToShow).ToString());
            }
            else
            {
                Console.WriteLine("Record to delete doesn't exist!");
            }
        }

        private List<int> RetriveIntsFromString(string sNumbers)
        {
            sNumbers = sNumbers.Remove(0, 2);   // deleting letter & space (ex.: "I ")
            return sNumbers.Split(' ').Select(Int32.Parse).ToList();
        }

        public static void ResetStaticValues()
        {
            nrOfPagesInPrimary = defaultNrOfPages;
            nrOfPagesInOverflow = (int)Math.Ceiling(defaultNrOfPages * sizeCoeff);
            nrOfPagesInIndex = (int)Math.Ceiling(nrOfPagesInPrimary / (double)bi);
            nrOfPagesInIndexOld = nrOfPagesInIndex;
            V = 0;
            N = 0;

            nrOfOperations = 0;
            fileSize = 0;
    }

    }
}
