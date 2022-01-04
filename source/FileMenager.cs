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

            List<Record> records = new List<Record>();

            for (int i = 0; i < DBMS.defaultNrOfPages * DBMS.recPerPage; i++)
            {
                records.Add(GetEmptyRecord());
            }

            WriteToFile(filePath, records);

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
        public MyFile GenerateFileFromConsole()
        {
            Console.WriteLine("\nInput several records in format: key 'space' nr 'space' nr 'enter'.");
            Console.WriteLine("To finish type 'q'");
            string userInput;
            List<Record> userRecords = new List<Record>();

            while (true)
            {
                userInput = Console.ReadLine();
                if (userInput.Contains("q")) break;

                // checking overflow not implemented
                if (!RecordMenager.CheckRecordFormat(userInput))
                    Console.WriteLine("Input valid values!");
                else
                    userRecords.Add(RecordMenager.ParseStrToRecord(userInput));
            }

            MyFile consoleInputTape = new MyFile(testFile, userRecords);

            return consoleInputTape;
        }
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

        //
        // ? mozesz zmienic funkcje WriteToIndexFile na WriteToFile - chyba ze cos sie spiepszylo
        //
        public void WriteToIndexFile(string filePath, List<(int, int)> fileContent)
        {
            // writes list of tuples to file
            if (fileContent != null)
            {
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None))
                    using (BinaryWriter writer = new BinaryWriter(fileStream))
                    {
                        foreach (var tup in fileContent)
                        {
                            writer.Write(tup.Item1);
                            writer.Write(tup.Item2);
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
        public void WriteToFile(string filePath, List<Record> records)
        {

            if (records != null)
            {
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None))
                    {
                        using (BinaryWriter writer = new BinaryWriter(fileStream))
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
                    if (RecordMenager.CheckTestRecordFormat(record))
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
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        int cnt = 0;

                        while (br.BaseStream.Position != br.BaseStream.Length)
                        { 
                            if (cnt % DBMS.nrOfIntsInRecord == 0)
                            {
                                Console.WriteLine();
                                if(cnt % (DBMS.defaultNrOfPages * DBMS.recPerPage) == 0) {
                                    cnt = 0;
                                    Console.WriteLine("-----------------------");
                                }                             
                            }

                            Console.Write(BitConverter.ToInt32(br.ReadBytes(4), 0) + " ");
                            cnt++;
                        }
                    }
                }
                Console.WriteLine();
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
            return indexFile;
        }
        
        public static string GetOverflowFileName()
        {
            return indexFile;
        }
        public static string GetTestFileName()
        {
            return testFile;
        }
    }


}
