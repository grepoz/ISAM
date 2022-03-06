using System;
using System.Collections.Generic;
using System.IO;

namespace ISAM.source
{
    internal class FileMenager
    {
        private const string Ext = ".bin";
        private static readonly string DirPath = Directory.GetCurrentDirectory() + @"\files";
        private static readonly string IndexFile = CreateFilePath("index");
        private static readonly string PrimaryFile = CreateFilePath("primary");
        private static readonly string OverflowFile = CreateFilePath("overflow");
        private static readonly string TestFile = DirPath + @"\" + "test.txt";
        private const string Attr = "_new";
        private static readonly string IndexNewFile = CreateFilePath("index", Attr);
        private static readonly string PrimaryNewFile = CreateFilePath("primary", Attr);
        private static readonly string OverflowNewFile = CreateFilePath("overflow", Attr);

        public FileMenager()
        {
            CreateDirectory();
            GenerateIndexFile(IndexFile, Dbms.DefaultNrOfPages);
            GenerateAreaFile(PrimaryFile, Dbms.DefaultNrOfPages);
            GenerateAreaFile(OverflowFile, Dbms.DefaultNrOfPages);
        }

        public static void GenerateAreaFile(string filePath, int nrOfPages)
        {
            CreateFile(filePath);

            var records = Page.InitArrayOfRecords(Dbms.bf);

            // oblicz bf dynamicznie
            for (var i = 0; i < nrOfPages; i++)
            {
                WriteToFile(filePath, records, i * Dbms.B);
            }

        }

        private static void CreateDirectory()
        {
            try
            {
                Directory.CreateDirectory(DirPath);
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e);
            }
        }


        public static void GenerateIndexFile(string filePath, int nrOfPages = 0)
        {
            if (nrOfPages <= 0) return;

            CreateFile(filePath);

            var fileContent = new List<(int, int)>();
            for (var j = 0; j < Dbms.nrOfPagesInPrimary; j++)
            {
                fileContent.Add((j * 10 + 1, j + 1));
            }
    
            WriteToIndexFile(filePath, fileContent);
        }
        public static void CreateFile(string filePath)
        {
            try
            {
                File.Create(filePath).Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        internal void DeleteFiles()
        {
            File.Delete(IndexFile);
            File.Delete(PrimaryFile);
            File.Delete(OverflowFile);
        }

        public static void WriteToIndexFile(string filePath, List<(int, int)> fileContent, int position = 0)
        {
            // writes list of tuples to file
            if (fileContent != null)
            {
                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        fs.Seek(position, SeekOrigin.Begin);
                        using (var writer = new BinaryWriter(fs))
                        {
                            foreach (var (item1, item2) in fileContent)
                            {
                                writer.Write(item1);
                                writer.Write(item2);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("Cannot write tuplee to tape!");
                }
            }
            else
                throw new InvalidOperationException("Variable fileContent is empty!");
        }
        public static void WriteToFile(string filePath, Record[] records, int position = 0)
        {

            if (records != null)
            {
                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        fs.Seek(position, SeekOrigin.Begin);
                        using (var writer = new BinaryWriter(fs))
                        {
                            foreach (var record in records)
                            {
                                var recordAsIntArr = record.ToIntArr();
                                foreach (var item in recordAsIntArr)
                                {
                                    writer.Write(item);
                                }
                            }
                            Dbms.nrOfOperations++;
                        }
                    }
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("Cannot write records to tape!");
                }
            }
            else
                throw new InvalidOperationException("Variable fileContent is empty!");
        }

        public List<string> ReadTestFile(string filePath = "")
        {
            if (filePath == "") filePath = TestFile;

            var commands = new List<string>();

            // whole content of test file is readed
            if (!File.Exists(filePath)) throw new InvalidOperationException("File does not!");
            commands.AddRange(File.ReadLines(filePath));

            return commands;
        }
        public string GetIndexFileName() => IndexFile;
        public string GetPrimaryFileName() => PrimaryFile;
        public string GetOverflowFileName() => OverflowFile;

        public string GetIndexNewFileName() => IndexNewFile;
        public string GetPrimaryNewFileName() => PrimaryNewFile;
        public string GetOverflowNewFileName() => OverflowNewFile;

        public static string GetExt() => Ext;
        public static string GetDirPath() => DirPath;
        public static string GetTestFileName() => TestFile;

        public static string CreateFilePath(string fileName, string attr = "")
        {
            return GetDirPath() + @"\" + fileName + attr + GetExt();
        }

        public static long GetFileSize()
        {
            var fiArray = new FileInfo[3];

            fiArray[0] = new FileInfo(IndexFile);
            fiArray[1] = new FileInfo(PrimaryFile);
            fiArray[2] = new FileInfo(OverflowFile);

            long wholeFileSize = 0;
            try
            {
                foreach (var fi in fiArray)
                {
                    wholeFileSize += fi.Length;
                }
            }
            catch (Exception)
            {
                throw new InvalidOperationException("cannot count size of file!");
            }
            
            // returning value in kB
            return wholeFileSize / 1000;
        }

    }


}
