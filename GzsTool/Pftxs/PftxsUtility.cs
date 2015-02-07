﻿using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GzsTool.Pftxs.Psub;

namespace GzsTool.Pftxs
{
    internal static class PftxsUtility
    {
        public static void UnpackPftxFile(string path)
        {
            string archiveOutputDirectoryName = string.Format("{0}_pftxs", Path.GetFileNameWithoutExtension(path));
            string archiveNameWithExtension = Path.GetFileName(path);
            string archiveDirectory = Path.GetDirectoryName(path);

            PftxsFile pftxsFile;
            using (FileStream input = new FileStream(path, FileMode.Open))
            {
                pftxsFile = PftxsFile.ReadPftxsFile(input);
            }
            PftxsLogFile logFile = new PftxsLogFile {ArchiveName = archiveNameWithExtension};
            string fileDirectory = "";
            foreach (var file in pftxsFile.FilesEntries)
            {
                var fileName = "";
                if (file.FileName.StartsWith("@"))
                {
                    fileName = file.FileName.Remove(0, 1);
                }
                else if (file.FileName.StartsWith("/"))
                {
                    fileDirectory = Path.GetDirectoryName(file.FileName.Remove(0, 1));
                    fileName = Path.GetFileName(file.FileName);
                }
                string relativeOutputDirectory = Path.Combine(archiveOutputDirectoryName, fileDirectory);
                string relativePath = Path.Combine(relativeOutputDirectory, fileName);
                string relativeFilePath = String.Format("{0}.ftex", relativePath);

                string fullOutputDirectory = Path.Combine(archiveDirectory, relativeOutputDirectory);
                Directory.CreateDirectory(fullOutputDirectory);
                string fullFilePath = Path.Combine(archiveDirectory, relativeFilePath);
                using (FileStream fileOutputStream = new FileStream(fullFilePath, FileMode.Create))
                {
                    fileOutputStream.Write(file.Data, 0, file.Data.Length);
                    Console.WriteLine(relativeFilePath);
                }
                int subFileNumber = 1;
                foreach (var psubFileEntry in file.PsubFile.Entries)
                {
                    string relativeSubFilePath = String.Format("{0}.{1}.ftexs", relativePath, subFileNumber);
                    string fullSubFilePath = Path.Combine(archiveDirectory, relativeSubFilePath);

                    using (FileStream subFileOutputStream = new FileStream(fullSubFilePath, FileMode.Create))
                    {
                        subFileOutputStream.Write(psubFileEntry.Data, 0, psubFileEntry.Data.Length);
                        Console.WriteLine(relativeSubFilePath);
                    }
                    subFileNumber += 1;
                }
                PftxsLogEntry logEntry = new PftxsLogEntry
                {
                    FileDirectory = fileDirectory,
                    FileName = fileName,
                    SubFileCount = file.PsubFile.Entries.Count()
                };
                logFile.Entries.Add(logEntry);
            }

            using (FileStream xmlStream =
                new FileStream(Path.Combine(archiveDirectory, string.Format("{0}.xml", archiveNameWithExtension)),
                    FileMode.Create))
            {
                var xmlSerializer = new XmlSerializer(typeof (PftxsLogFile));
                xmlSerializer.Serialize(xmlStream, logFile);
            }
        }

        public static void PackPftxFile(string path)
        {
            string archiveDirectory = Path.GetDirectoryName(path);
            string archiveName = Path.GetFileNameWithoutExtension(path);
            PftxsLogFile logFile = ReadPftxsLogFile(path);
            PftxsFile pftxsFile = ConvertToPftxs(logFile, archiveDirectory);
            using (FileStream output = new FileStream(Path.Combine(archiveDirectory, archiveName), FileMode.Create))
            {
                pftxsFile.Write(output);
            }
        }

        private static PftxsFile ConvertToPftxs(PftxsLogFile logFile, string workingDirectoryPath)
        {
            string archiveInputDirectoryName = string.Format("{0}_pftxs",
                Path.GetFileNameWithoutExtension(logFile.ArchiveName));
            PftxsFile pftxsFile = new PftxsFile();
            string lastDirectory = "";
            foreach (var logEntry in logFile.Entries)
            {
                PftxsFileEntry entry = new PftxsFileEntry();
                string relativePath;
                if (lastDirectory.Equals(logEntry.FileDirectory))
                {
                    entry.FileName = String.Format("@{0}", logEntry.FileName).Replace('\\', '/');
                    relativePath = Path.Combine(archiveInputDirectoryName, lastDirectory, logEntry.FileName);
                }
                else
                {
                    string arelativeFilePath = String.Format("{0}\\{1}", logEntry.FileDirectory,
                        logEntry.FileName);
                    entry.FileName = String.Format("\\{0}", arelativeFilePath).Replace('\\', '/');
                    lastDirectory = logEntry.FileDirectory;
                    relativePath = Path.Combine(archiveInputDirectoryName, arelativeFilePath);
                }
                string relativeFilePath = string.Format("{0}.ftex", relativePath);
                string fullFilePath = Path.Combine(workingDirectoryPath, relativeFilePath);
                entry.Data = File.ReadAllBytes(fullFilePath);
                Console.WriteLine(relativeFilePath);
                entry.FileSize = entry.Data.Length;

                PsubFile psubFile = new PsubFile();
                for (int i = 1; i <= logEntry.SubFileCount; i++)
                {
                    string relativeSubFilePath = String.Format("{0}.{1}.ftexs", relativePath, i);
                    string fullSubFilePath = Path.Combine(workingDirectoryPath, relativeSubFilePath);
                    var psubFileData = File.ReadAllBytes(fullSubFilePath);
                    Console.WriteLine(relativeSubFilePath);
                    PsubFileEntry psubFileEntry = new PsubFileEntry
                    {
                        Data = psubFileData,
                        Size = psubFileData.Length
                    };
                    psubFile.AddPsubFileEntry(psubFileEntry);
                }
                entry.PsubFile = psubFile;
                pftxsFile.AddPftxsFileEntry(entry);
            }
            pftxsFile.FileCount = pftxsFile.FilesEntries.Count();
            return pftxsFile;
        }

        private static PftxsLogFile ReadPftxsLogFile(string logFilePath)
        {
            PftxsLogFile logFile;
            using (FileStream xmlInput = new FileStream(logFilePath, FileMode.Open))
            {
                var xmlSerializer = new XmlSerializer(typeof (PftxsLogFile));
                logFile = xmlSerializer.Deserialize(xmlInput) as PftxsLogFile;
            }
            return logFile;
        }
    }
}