using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ISAM.source
{
    internal class Dbms
    {
        public const int B = 80;  // disk page capacity - nr of bytes readed at once - always must be multiple of 'R'
        public const int R = 20;   // size of record - 5* int 
        public const int K = 4;   // size of key - int 
        public const int P = 4;   // size of pointer - int 
        public double alpha = 0.75;   // page utlilization factor in the main area just after reorganization, α < 1
        public const int DefaultNrOfPages = 1;
        public const int NrOfIntsInRecord = R / 4;
        public double delta = 0.75;  // fullfillment of overflow   
        public const double SizeCoeff = 0.2;
        public static int bf = (int)Math.Floor((double)(B / R));    // attention! - Record includes 'P' !
        public static int bi = (int)Math.Floor((double)(B / (K + P)));  // = 8 

        // changes after reorganisation
        public static int nrOfPagesInPrimary = DefaultNrOfPages;
        public static int nrOfPagesInOverflow =  (int)Math.Ceiling(DefaultNrOfPages * SizeCoeff);
        public static int nrOfPagesInIndex = (int)Math.Ceiling(nrOfPagesInPrimary / (double)bi);
        public static int nrOfPagesInIndexOld = nrOfPagesInIndex;
        public static int V;
        public static int N;

        // stats
        public static int nrOfOperations;
        public static int fileSize;
        public static int nrOfReorg;

        public bool isDebug;

        private int _nextEmptyOverflowIndex;
        private readonly FileMenager _fm;

        public Dbms(FileMenager fm, bool isDebug = true)
        {
            SetParametersDynamically(alpha, delta);

            this.isDebug = isDebug;
            _fm = fm;
            _nextEmptyOverflowIndex = 0;

            InsertSpecialFirstRecord(fm.GetPrimaryFileName());
            N++;
        }

        public void SetParametersDynamically(double alphaArg, double deltaArg)
        {
            alpha = alphaArg;
            delta = deltaArg;
        }

        private static void InsertSpecialFirstRecord(string filePath)
        {
            if (File.Exists(filePath))
            {
                var toBeInserted = new Record(0, 0, 0, 1, -1);
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
                    using (var br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        if (br.BaseStream.Length <= 0) return null;

                        var chunk = br.ReadBytes(B); 
                        var page = RecordMenager.BytesToPage(chunk);
                        page.SetPageNr(position / B + 1);
                        page.SetPageFilePath(filePath);

                        nrOfOperations++;
                        return page;

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
                    using (var br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        if (br.BaseStream.Length <= 0) return new int[0, 0];

                        var chunk = br.ReadBytes(B);
                        return RecordMenager.BytesToIndexPage(chunk);

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot read a chunk from file:\n" + e.Message);
                return new int[0, 0];
            }
        }
        public void InsertRecord(Record toBeInserted)
        {
            if (IsRecordValid(toBeInserted))
            {
                var pageNr = GetPageNr(toBeInserted.GetKey());

                var page = ReadPage(_fm.GetPrimaryFileName(), (pageNr - 1) * B);

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

            for (var i = 0; i < page.MyGetLength(); i++)
            {
                var record = page.GetRecords().ElementAt(i);
                if (record.IsEmpty())
                {                    
                    page.ReplaceFirstEmpty(toBeInserted);
                    FileMenager.WriteToFile(_fm.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
                    N++;
                    return;                  
                }

                if(record.GetKey() < toBeInserted.GetKey())
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
                    if(record.Exist())
                        Console.WriteLine("Cannot insert or update record! Key duplicated!");
                    else
                    {
                        page.Get(toBeInserted.GetKey()).Update(toBeInserted);
                        page.Get(toBeInserted.GetKey()).SetDeleted(0);
                        FileMenager.WriteToFile(_fm.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
                        N++;
                    }
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
                FileMenager.WriteToFile(_fm.GetPrimaryFileName(), page.GetRecords(), (pageNr - 1) * B);
                N++;
            }
            // if function return earlier - record was inserted to overflow file,
            // otherwise record will be inserted to primary

            
        }
        private void InsertToOverflowFileAtEnd(Record toBeInserted)
        {
            var inserted = false;

            for (var pageNr = 0; pageNr < nrOfPagesInOverflow && !inserted; pageNr++)
            {
                var page = ReadPage(_fm.GetOverflowFileName(), pageNr * B);

                if (page.IsFull()) continue;

                page.ReplaceFirstEmpty(toBeInserted);
                FileMenager.WriteToFile(_fm.GetOverflowFileName(), page.GetRecords(), pageNr * B);
                inserted = true;
                V++;

                if(page.IsFull() && pageNr + 1 == nrOfPagesInOverflow) Reorganise();
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
                prevRecord.SetNext(_nextEmptyOverflowIndex++);
                FileMenager.WriteToFile(_fm.GetPrimaryFileName(), page.GetRecords(), (page.nr - 1) * B);

                InsertToOverflowFileAtEnd(toBeInserted);
            }
            else
            {
                var overflowPageNr = GetOverflowPageNr(prevRecord.GetNext());
                var overflowPage = ReadPage(_fm.GetOverflowFileName(), (overflowPageNr -1) * B);

                var firstRecordInOverflow = overflowPage.GetRecords().ElementAt(GetOverflowRecIndexInPage(prevRecord.GetNext()));

                if (firstRecordInOverflow.GetKey() < toBeInserted.GetKey())
                {
                    firstRecordInOverflow.SetNext(_nextEmptyOverflowIndex++);
                    firstRecordInOverflow.WriteRecToFile(_fm.GetOverflowFileName(), prevRecord.GetNext());

                    InsertToOverflowFileAtEnd(toBeInserted);
                }
                else if (firstRecordInOverflow.GetKey() == toBeInserted.GetKey())
                {
                    if(firstRecordInOverflow.Exist())
                        Console.WriteLine("Key of inserted record duplicated! Insert rejected!");
                    else
                    {
                        firstRecordInOverflow = toBeInserted;
                        firstRecordInOverflow.WriteRecToFile(_fm.GetOverflowFileName(), prevRecord.GetNext());
                        V++;
                    }
                }
                else
                {
                    // swap
                    Record toBeSwapped = new Record(firstRecordInOverflow);
                    firstRecordInOverflow.Update(toBeInserted);
                    firstRecordInOverflow.SetNext(_nextEmptyOverflowIndex++);
                    firstRecordInOverflow.WriteRecToFile(_fm.GetOverflowFileName(), prevRecord.GetNext());
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

            var page = GetPage(keyOfRecToUpdate);

            if (page != null)
            {
                var toBeUpdated = page.Get(keyOfRecToUpdate);
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
                        var potentialyYetInDb = GetRecord(freshRecord.GetKey());
                        if (potentialyYetInDb == null)
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
            if(pageFilePath == _fm.GetPrimaryFileName())
            {
                N--;
            }
            else if(pageFilePath == _fm.GetOverflowFileName())
            {
                V--;
            }
        }
        private Page GetPage(int keyOfRecToFind)
        {
            // returns null if key was not found
            var pageNr = GetPageNr(keyOfRecToFind);

            var page = ReadPage(_fm.GetPrimaryFileName(), (pageNr - 1) * B);

            var recToFind = page.Get(keyOfRecToFind);

            if(recToFind != null) return page;

            // we have to check if record is in overflow
            Record prevRecord = null;

            foreach (var record in page.GetRecords())
            {
                if (record.IsEmpty())
                {
                    return null;
                }

                if (record.GetKey() < keyOfRecToFind)
                {
                    prevRecord = record;
                }
                else if (record.GetKey() > keyOfRecToFind)
                {

                    return GetPageFromOverflow(prevRecord, keyOfRecToFind);
                }
            }
            return null;
        }
        private Page GetPageFromOverflow(Record prevRecord, int keyOfRecToFind)
        {
            if (prevRecord == null) throw new InvalidOperationException("uninitialised!");

            var isFound = false;
            Page overflowPage = null;
            var isNextRecordOnReadPage = false;
            while (!isFound)
            {
                var indexInOverflow = prevRecord.GetNext();
                if (indexInOverflow == -1) break;

                if (!isNextRecordOnReadPage)
                {
                    var overflowPageNr = GetOverflowPageNr(indexInOverflow);
                    overflowPage = ReadPage(_fm.GetOverflowFileName(), (overflowPageNr - 1) * B);
                }

                var indexInPage = indexInOverflow % bf;

                var toFound = overflowPage.GetRecords().ElementAt(indexInPage);

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
        private static bool IsNextRecordOnReadPage(int anchorIndex, int nextRecIndex)
        {
            var pageNr = anchorIndex / bf;

            return nextRecIndex >= anchorIndex && nextRecIndex < pageNr * (bf + 1);
        }
        private void Delete(int keyOfRecToBeDeleted)
        {
            var page = GetPage(keyOfRecToBeDeleted);
            if(page != null)
            {
                var toDelete = page.Get(keyOfRecToBeDeleted);
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
            nrOfReorg++;

            Console.WriteLine($"\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" +
                $"~~~~~~ Reorganisation ~~~~~~\n" +
                $"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");

            if (isDebug)
            {
                DisplayIndexFileContent(_fm.GetIndexFileName());
                DisplayFileContent(_fm.GetPrimaryFileName());
                DisplayFileContent(_fm.GetOverflowFileName());

                Console.WriteLine($"N: {N}");
                Console.WriteLine($"V: {V}");
            }

            var nrOfPagesInOldPrimary = nrOfPagesInPrimary;
            GenerateFilesToReorganisation();
            InsertSpecialFirstRecord(_fm.GetPrimaryNewFileName());

            var pageNrInOldPrimary = 0;
            var pageNrInNewPrimary = 0;
            var pageNrInNewIndex = 0;
            var pageNrInIndex = 1;

            var newIndexPage = new List<(int key, int pageNr)>();
            const int firstKeyInIndex = 1;
            newIndexPage.Add((firstKeyInIndex, pageNrInIndex++));

            var newPrimaryPage = ReadPage(_fm.GetPrimaryNewFileName(), /*pageNrInNewPrimary * */B);

            var indexPageInserted = false;

            while (pageNrInOldPrimary< nrOfPagesInOldPrimary && pageNrInNewPrimary < nrOfPagesInPrimary)
            {
                indexPageInserted = false;

                var oldPrimaryPage = ReadPage(_fm.GetPrimaryFileName(), pageNrInOldPrimary * B);

                foreach (var oldRecord in oldPrimaryPage.GetRecords())
                {
                    if (oldRecord.IsEmpty()) break;
                    foreach (var item in GetOverflowChain(oldRecord.GetKey()))
                    {
                        if (!item.Exist()) continue;

                        if (newPrimaryPage.MyGetLength() >= (int)Math.Ceiling(alpha * bf))
                        {   
                            newIndexPage.Add((key: item.GetKey(), pageNr: pageNrInIndex++));
                            if (newIndexPage.Count == bi)
                            {
                                FileMenager.WriteToIndexFile(_fm.GetIndexNewFileName(), newIndexPage, pageNrInNewIndex * B);
                                pageNrInNewIndex++;
                                newIndexPage.Clear();
                                indexPageInserted = true;
                            }

                            // we save fullfilled page 
                            FileMenager.WriteToFile(_fm.GetPrimaryNewFileName(), newPrimaryPage.GetRecords(), pageNrInNewPrimary * B);
                            pageNrInNewPrimary++;
                            newPrimaryPage = ReadPage(_fm.GetPrimaryNewFileName(), pageNrInNewPrimary * B);
                        }
                        item.SetNext(-1);
                        newPrimaryPage.ReplaceFirstEmpty(item);
                    }
                }
                pageNrInOldPrimary++;
            }

            // always save not full pages
            if (newPrimaryPage != null && !newPrimaryPage.IsEmpty())
            {
                FileMenager.WriteToFile(_fm.GetPrimaryNewFileName(), newPrimaryPage.GetRecords(), pageNrInNewPrimary * B);
            }

            if (!indexPageInserted)
            {
                FileMenager.WriteToIndexFile(_fm.GetIndexNewFileName(), newIndexPage, pageNrInNewIndex * B);
            }

            // menage files and set counters
            DeleteOldFiles();
            RenameFilesAfterReorganisation();

            N += V;
            V = 0;
            
            _nextEmptyOverflowIndex = 0;
            nrOfPagesInIndexOld = nrOfPagesInIndex;

            Console.WriteLine("\n~~~~ After reorganisation! ~~~~\n");
        }
        private void DeleteOldFiles()
        {
            File.Delete(_fm.GetIndexFileName());
            File.Delete(_fm.GetPrimaryFileName());
            File.Delete(_fm.GetOverflowFileName());
        }
        public void GenerateFilesToReorganisation()
        {
            nrOfPagesInPrimary = (int)Math.Ceiling(((double)N + V) / (bf * alpha));
            nrOfPagesInIndexOld = nrOfPagesInIndex;
            nrOfPagesInIndex = (int)Math.Ceiling(nrOfPagesInPrimary / (double)bi);
            nrOfPagesInOverflow = (int)Math.Ceiling(nrOfPagesInPrimary * SizeCoeff);

            FileMenager.GenerateIndexFile(_fm.GetIndexNewFileName() /*, nrOfPagesInIndex*/);
            FileMenager.GenerateAreaFile(_fm.GetPrimaryNewFileName(), nrOfPagesInPrimary);
            FileMenager.GenerateAreaFile(_fm.GetOverflowNewFileName(), nrOfPagesInOverflow);
        }
        private void RenameFilesAfterReorganisation()
        {

            File.Move(_fm.GetIndexNewFileName(), _fm.GetIndexFileName());
            File.Move(_fm.GetPrimaryNewFileName(), _fm.GetPrimaryFileName());
            File.Move(_fm.GetOverflowNewFileName(), _fm.GetOverflowFileName());


        }
        private bool IsReorganisation()
        {   
            return V / (double)N >= delta;           
        }
        private static int GetOverflowRecIndexInPage(int globalIndex)
        {
            return globalIndex % bf;
        }

        private int GetPageNr(int key)
        {
            var pageIndex = 0;

            var pageNr = 0;

            while (pageIndex < nrOfPagesInIndexOld)    // bylo nrOfPagesInIndex
            {
                var indexPage = ReadIndexPage(_fm.GetIndexFileName(), pageIndex * B);

                for (var i = 0; i < indexPage.GetLength(0); i++)
                {
                    var indexKey = indexPage[i, 0];

                    if (key == 0) return 1;
                    if(indexKey > key) return pageNr;

                    pageNr++;
                }
                pageIndex++;
            }

            return pageNr;
        }
        private static bool IsRecordValid(Record record)
        {
            return record.GetKey() > 0 && record.GetData1() > 0 && record.GetData2() > 0 && 
                   record.GetDeleted() != 1 && record.GetNext() == -1;
        }
        public void DisplayDbAscending()
        {
            for (var pageNr = 0; pageNr < nrOfPagesInPrimary; pageNr++)
            {
                var page = ReadPage(_fm.GetPrimaryFileName(), pageNr * B);

                foreach (var record in page.GetRecords())
                {
                    if (!record.IsEmpty())
                    {
                        Console.WriteLine(record.ToString());
                        if (record.GetNext() != -1)
                        {
                            // we know that record points to next record
                            DisplayOverflowChain(record);
                        }
                    }
                    else
                    {
                        // we know that first record is empty so we know that page is empty,
                        // but next pages may contain records
                        break;
                    }
                }
            }

        }
        private void DisplayOverflowChain(Record anchor)
        {
            // could do better - always opening page instead of search through whole page
            var endOfChain = false;
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
            var fileName = Path.GetFileName(filePath);
            if (filePath == _fm.GetIndexFileName())
            {
                DisplayIndexFileContent(filePath);
            }
            else
            {
                Console.WriteLine($"###### {fileName} ######");
                for (var position = 0; position < GetNrOfPagesOfFile(filePath); position++)
                {
                    Console.WriteLine($"------ Page: {position + 1} ------");
                    var page = ReadPage(filePath, position * B);

                    page.GetRecords().ToList().ForEach(record => Console.Write(record.ToString()));
                }
                Console.WriteLine();
            }
        }
        public void DisplayIndexFileContent(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            Console.WriteLine($"###### {fileName} ######");
            for (var position = 0; position < GetNrOfPagesOfFile(filePath); position++)
            {
                Console.WriteLine($"------ Page: {position + 1} ------");
                var page = ReadIndexPage(filePath, position * B);
                for (var i = 0; i < page.GetLength(0); i++)
                {                 
                    Console.WriteLine($"[ key: {page[i, 0]}, page: {page[i, 1]} ]");
                }
            }
            Console.WriteLine();
        }
        private List<Record> GetOverflowChain(int anchorKey)
        {
            var chain = new List<Record>();
            var anchor = GetRecord(anchorKey);

            // with pre..pre anchor
            chain.Add(anchor);


            var endOfChain = false;
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
            var pageNr = GetPageNr(keyOfRecToFind);

            var page = ReadPage(_fm.GetPrimaryFileName(), (pageNr - 1) * B);

            Record prevRecord = null;

            foreach (var record in page.GetRecords())
            {
                if (record.IsEmpty() && record.GetDeleted() == 0)
                {
                    return null;
                }

                if (record.GetKey() < keyOfRecToFind)
                {
                    prevRecord = record;
                }
                else if (record.GetKey() == keyOfRecToFind)
                {
                    return record;
                }
                else
                {
                    return GetRecordFromOverflow(prevRecord, keyOfRecToFind);
                }
            }

            return null;
        }
        public Record GetNextRecordFromOverflow(Record prevRecord)
        {
            if (prevRecord == null) throw new InvalidOperationException("uninitialised!");
            var isFound = false;
            Record toFound = null;
            while (!isFound)
            {
                var indexInOverflow = prevRecord.GetNext();

                var overflowPageNr = GetOverflowPageNr(indexInOverflow);
                const int overflowPageNrSecondAttempt = -1;
                Page overflowPage = null;
                if (overflowPageNr != overflowPageNrSecondAttempt)
                {
                    overflowPage = ReadPage(_fm.GetOverflowFileName(), (overflowPageNr - 1) * B);
                }

                var indexInPage = indexInOverflow % bf;
                if (overflowPage != null) toFound = overflowPage.GetRecords().ElementAt(indexInPage);

                if (toFound != null && toFound.IsEmpty())
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
            var isFound = false;
            Record toFound = null;
            var overflowPageNrSecondAttempt = -1;
            while (!isFound)
            {
                var indexInOverflow = prevRecord.GetNext();
                if (indexInOverflow == -1) break;
                var overflowPageNr = GetOverflowPageNr(indexInOverflow);
                
                Page overflowPage = null;
                if (overflowPageNr != overflowPageNrSecondAttempt)
                {
                    overflowPage = ReadPage(_fm.GetOverflowFileName(), (overflowPageNr - 1) * B);
                }

                var indexInPage = indexInOverflow % bf;
                if (overflowPage != null) toFound = overflowPage.GetRecords().ElementAt(indexInPage);

                if (toFound != null && !toFound.IsEmpty())
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
                }
                else
                {
                    return null;
                }
            }
            return toFound;

        }
        private static int GetOverflowPageNr(int indexInOverflow)
        {
            return (int)Math.Ceiling((indexInOverflow + 1.0f) / (double)bf);
        }
        public int GetNrOfPagesOfFile(string filePath)
        {
            if (filePath == _fm.GetPrimaryFileName())
                return nrOfPagesInPrimary;
            if (filePath == _fm.GetOverflowFileName())
                return nrOfPagesInOverflow;
            if (filePath == _fm.GetIndexFileName())
                return nrOfPagesInIndex;
            throw new InvalidOperationException("Cannot get nr of pages!");
        }
        public void CmdHandler(string[] cmds)
        {

            foreach (var cmd in cmds)
            {
                Console.WriteLine(
                    "\n   ******************\n" +
                    $"   * cmd: {cmd} *\n" +
                    "   ******************\n");
              
                CmdInterpreter(cmd);

                if (!isDebug) continue;

                DisplayIndexFileContent(_fm.GetIndexFileName());
                DisplayFileContent(_fm.GetPrimaryFileName());
                DisplayFileContent(_fm.GetOverflowFileName());

                Console.WriteLine($"N: {N}");
                Console.WriteLine($"V: {V}");

            }
        }

        public void CmdInterpreter(string cmd)
        {
            if (cmd.Contains("I "))
            {
                var rx = new Regex(@"^I [0-9]*$");
                if (rx.IsMatch(cmd))
                {
                    var recData = RetriveIntsFromString(cmd);
                    var record = new Record(recData[0], 1, 1);
                    InsertRecord(record);
                }
                else Console.WriteLine("Wrong command!");
            }
            else if (cmd.Contains("U "))
            {
                var rx = new Regex(@"^U [0-9]* [0-9]* [0-9]* [0-9]*$");
                if (rx.IsMatch(cmd))
                {
                    var recData = RetriveIntsFromString(cmd);
                    var keyOfRecToUpdate = recData[0];
                    var freshRecord = new Record(recData[1], recData[2], recData[3]);
                    Update(keyOfRecToUpdate, freshRecord);
                }
                else Console.WriteLine("Wrong command!");
            }
            else if (cmd.Contains("D "))
            {
                var rx = new Regex(@"^D [0-9]*$");
                if (rx.IsMatch(cmd))
                {
                    var recData = RetriveIntsFromString(cmd);
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
                DisplayDbAscending();
            }
            else if (cmd.Contains("S "))
            {
                var rx = new Regex(@"^S [0-9]*$");
                if (rx.IsMatch(cmd))
                {
                    var recData = RetriveIntsFromString(cmd);
                    var keyOfRecToShow = recData[0];
                    ShowRecord(keyOfRecToShow);
                }
                else Console.WriteLine("Wrong command!");
            }
            else Console.WriteLine("Wrong command!");
        }
        private void ShowRecord(int keyOfRecToShow)
        {
            var page = GetPage(keyOfRecToShow);
            Console.WriteLine(page != null ? page.Get(keyOfRecToShow).ToString() : "Record to delete doesn't exist!");
        }
        private static List<int> RetriveIntsFromString(string sNumbers)
        {
            sNumbers = sNumbers.Remove(0, 2);   // deleting letter & space (ex.: "I ")
            return sNumbers.Split(' ').Select(int.Parse).ToList();
        }
        public static void ResetStaticValues()
        {
            nrOfPagesInPrimary = DefaultNrOfPages;
            nrOfPagesInOverflow = (int)Math.Ceiling(DefaultNrOfPages * SizeCoeff);
            nrOfPagesInIndex = (int)Math.Ceiling(nrOfPagesInPrimary / (double)bi);
            nrOfPagesInIndexOld = nrOfPagesInIndex;
            V = 0;
            N = 0;

            nrOfOperations = 0;
            fileSize = 0;
    }

    }
}
