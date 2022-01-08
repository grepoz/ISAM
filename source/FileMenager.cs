using ISFO.source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using static ISFO.MyFile;

namespace ISFO
{
    class FileMenager
    {
        private const string ext = ".bin";
        private static readonly string dirPath = Directory.GetCurrentDirectory().ToString() + @"\files";
        private static readonly string indexFile = CreateFilePath("index");
        private static readonly string primaryFile = CreateFilePath("primary");
        private static readonly string overflowFile = CreateFilePath("overflow");
        private static readonly string testFile = dirPath + @"\" + "test.txt";
        const string attr = "_new";
        private static readonly string indexNewFile = CreateFilePath("index", attr);
        private static readonly string primaryNewFile = CreateFilePath("primary", attr);
        private static readonly string overflowNewFile = CreateFilePath("overflow", attr);


        public FileMenager()
        {
            CreateDirectory();
            GenerateIndexFile(indexFile, DBMS.defaultNrOfPages);
            GenerateAreaFile(primaryFile, DBMS.defaultNrOfPages);
            GenerateAreaFile(overflowFile, DBMS.defaultNrOfPages);
        }

        public static void GenerateAreaFile(string filePath, int nrOfPages)
        {
            CreateFile(filePath);

            Record[] records = Page.InitArrayOfRecords(DBMS.bf);

            // oblicz bf dynamicznie
            for (int i = 0; i < nrOfPages; i++)
            {
                WriteToFile(filePath, records, i * DBMS.B);
            }

        }

        private static void CreateDirectory()
        {
            try
            {
                if (Directory.Exists(dirPath))
                    Console.WriteLine("Directory already exists!\n");

                DirectoryInfo di = Directory.CreateDirectory(dirPath);
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
        }
        /*public MyFile GenerateFileFromConsole()
        {
            Console.WriteLine("\nInput several records in format: key 'space' nr 'space' nr 'enter'.");
            Console.WriteLine("To finish type 'q'");
            string userInput;
            Record[] userRecords = Page.InitArrayOfRecords(DBMS.bf);

            while (true)
            {
                userInput = Console.ReadLine();
                if (userInput.Contains("q")) break;

                // checking overflow not implemented
                if (!RecordMenager.IsRecordFormatValid(userInput))
                    Console.WriteLine("Input valid values!");
                else
                    userRecords.Add(RecordMenager.ParseStrToRecord(userInput));
            }

            MyFile consoleInputTape = new MyFile(testFile, userRecords);

            return consoleInputTape;
        }
        */

        public static void GenerateIndexFile(string filePath, int nrOfPages, bool isEmpty = false)
        {
            CreateFile(filePath);

            if(!isEmpty)
            {
                // default distribution

                List<(int, int)> fileContent = new List<(int, int)>();
                for (int i = 0; i < nrOfPages; i++)
                {
                    fileContent.Add((i * 10 + 1, i + 1));
                }

                WriteToIndexFile(filePath, fileContent);
            }

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
                        using (BinaryWriter writer = new BinaryWriter(fs))
                        {
                            foreach (var tup in fileContent)
                            {
                                writer.Write(tup.Item1);
                                writer.Write(tup.Item2);
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
                        using (BinaryWriter writer = new BinaryWriter(fs))
                        {
                            foreach (var record in records)
                            {
                                var recordAsIntArr = record.ToIntArr();
                                foreach (var item in recordAsIntArr)
                                {
                                    writer.Write(item);
                                }
                            }
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
            if (filePath == "") filePath = testFile;

            List<string> commands = new List<string>();

            // whole content of test file is readed
            if (File.Exists(filePath))
            {
                foreach (string record in System.IO.File.ReadLines(filePath))
                {
                    if (RecordMenager.IsTestRecordFormatValid(record))
                        commands.Add(record);
                    else
                        throw new InvalidOperationException("Invalid record format!");
                }

                return commands;
            }
            else
            {
                throw new InvalidOperationException("File does not!");
            }
        }
        public string GetIndexFileName() => indexFile;
        public string GetPrimaryFileName() => primaryFile;
        public string GetOverflowFileName() => overflowFile;

        public string GetIndexNewFileName() => indexNewFile;
        public string GetPrimaryNewFileName() => primaryNewFile;
        public string GetOverflowNewFileName() => overflowNewFile;

        public static string GetExt() => ext;
        public static string GetDirPath() => dirPath;
        public static string GetTestFileName() => testFile;

        public static string CreateFilePath(string fileName, string attr = "")
        {
            return GetDirPath() + @"\" + fileName + attr + GetExt();
        }

    }


}
