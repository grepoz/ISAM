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
        private static readonly string indexFile = dirPath + @"\" + "index" + ext;
        private static readonly string primaryFile = dirPath + @"\" + "primary" + ext;
        private static readonly string overflowFile = dirPath + @"\" + "overflow" + ext;
        private static readonly string testFile = dirPath + @"\" + "test" + ext;

        public FileMenager()
        {
            CreateDirectory();
            GenerateBasicIndexFile();
            GenerateAreaFile(primaryFile);
            GenerateAreaFile(overflowFile);
        }

        private void GenerateAreaFile(string filePath)
        {
            CreateFile(filePath);

            Record[] records = Page.InitArrayOfRecords(DBMS.recPerPage);
            // oblicz recPerPage dynamicznie
            for (int i = 0; i < DBMS.defaultNrOfPages; i++)
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
            Record[] userRecords = Page.InitArrayOfRecords(DBMS.recPerPage);

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

        private void GenerateBasicIndexFile()
        {
            CreateFile(indexFile);

            List<(int, int)> fileContent = new List<(int, int)>();

            for (int i = 0; i < DBMS.defaultNrOfPages; i++)
            {
                fileContent.Add((i * 10 + 1, i + 1));
            }

            WriteToIndexFile(indexFile, fileContent);
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


        public void DisplayFileContent(string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        int cnt = 0, pageNr = 0;

                        int nrOfPages;
                        if (filePath == primaryFile) nrOfPages = DBMS.nrOfPageInPrimary;
                        else nrOfPages = DBMS.nrOfPageInOverflow;

                        string fileName = Path.GetFileName(filePath);
                        Console.WriteLine("###### {0} ######", fileName);

                        while (br.BaseStream.Position != br.BaseStream.Length)
                        { 
                            if (cnt % DBMS.nrOfIntsInRecord == 0)
                            {
                                Console.WriteLine();
                                // error defaultNrOfPages is changing

                                // display 
                                if (cnt % (DBMS.nrOfIntsInRecord * DBMS.recPerPage) == 0) {
                                    cnt = 0;
                                    Console.WriteLine("--------- Page " + (pageNr++ + 1) + " ---------");

                                    if (pageNr % nrOfPages == 0) pageNr = 0;
                                }                             
                            }

                            Console.Write(BitConverter.ToInt32(br.ReadBytes(4), 0) + " ");
                            cnt++;
                        }
                    }
                }
                Console.WriteLine("\n==========================\n");
            }
            catch (Exception)
            {
                throw new InvalidOperationException("File is empty or does not exist!");
            }

        }




        public static string GetIndexFileName()
        {
            return indexFile;
        }

        public static string GetPrimaryFileName()
        {
            return primaryFile;
        }
        
        public static string GetOverflowFileName()
        {
            return overflowFile;
        }
        public static string GetTestFileName()
        {
            return testFile;
        }
    }


}
