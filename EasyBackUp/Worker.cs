using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EasyBackUp
{
    public class Worker
    {
        //todo populate from config
        private List<TargetDefinition> _definitions = new()
        {
            new TargetDefinition {
                TargetFolder = @"D:\Users\me\Documents\My Games\XCOM - Enemy Within\XComGame\SaveData\",
                Glob = "save5",
                BackupFolder = @"D:\Users\me\Documents\My Games\XCOM - Enemy Within\XComGame\SaveData\Backup\",
                Interval = TimeSpan.FromMinutes(60),
                MaxBackups = 10 }
        };

        public async Task ExecuteAsync(CancellationToken? cancellationToken = null)
        {
            while (cancellationToken?.IsCancellationRequested != true)
            {
                var logPath = @"C:\Users\me\source\repos\EasyBackUp\EasyBackUp\bin\Debug\test.log";
                File.AppendAllText(logPath, $"{DateTime.Now:s} Running\r\n");

                foreach (var definition in _definitions)
                {
                    await Task.Run(() => Process(definition));
                }
            }
        }

        private static void Process(TargetDefinition definition)
        {
            //for each file:
            //  find latest backup
            //  check if the file has changed and grace period has passed
            //    if yes, make a new backup
            //  check if there are more bacups than allowed
            //    if yes, delete oldest backups until there are as many as allowed

            var targetDirectory = new DirectoryInfo(definition.TargetFolder);
            var files = targetDirectory.GetFiles(definition.Glob);
            var backupDirectory = new DirectoryInfo(definition.BackupFolder);

            if (!backupDirectory.Exists)
            {
                backupDirectory.Create();
            }

            foreach (var file in files)
            {
                var existingBackups = GetExistingBackups(backupDirectory, file);

                var latestBackup = existingBackups.OrderByDescending(t => t.backupNumber).FirstOrDefault();

                if (latestBackup.file == null || file.LastWriteTimeUtc > latestBackup.file.LastWriteTimeUtc)
                {
                    Archive(file, backupDirectory, latestBackup.backupNumber + 1);
                }

                Prune(existingBackups, definition.MaxBackups);
            }
        }

        private static ICollection<(int backupNumber, FileInfo file)> GetExistingBackups(DirectoryInfo backupDirectory, FileInfo file)
        {
            var backupBaseName = GetBackupBaseName(file);
            var existingBackups = new List<(int backupNumber, FileInfo file)>();

            //\d+_(\d+)_MyFile\.docx_bak\.zip
            var regex = new Regex($"\\d+_(\\d+)_{Regex.Escape(backupBaseName)}\\.zip");

            foreach (var backup in backupDirectory.GetFiles($"*{backupBaseName}*"))
            {
                var matches = regex.Matches(backup.Name);
                if (matches.Any())
                {
                    var number = int.Parse(matches[0].Groups[1].Value);
                    existingBackups.Add((number, backup));
                }
            }

            return existingBackups;
        }

        private static void Archive(FileInfo file, DirectoryInfo backupDirectory, int nextNumber)
        {
            var newBackupName = GetNextBackupName(file, nextNumber);

            using var stream = new FileStream(Path.Combine(backupDirectory.FullName, newBackupName), FileMode.Create);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(file.FullName, file.Name, CompressionLevel.Fastest);
        }

        private static void Prune(ICollection<(int backupNumber, FileInfo file)> existingBackups, int maxBackups)
        {
            if (existingBackups.Count + 1 > maxBackups)
            {
                foreach (var item in existingBackups.OrderByDescending(kvp => kvp.backupNumber).Skip(maxBackups - 1))
                {
                    item.file.Delete();
                }
            }
        }

        private static string GetBackupBaseName(FileInfo file)
        {
            //MyFile.docx_bak
            return $"{file.Name}_bak";
        }

        private static string GetNextBackupName(FileInfo file, int nextNumber)
        {
            //20220215_59_MyFile.docx_bak.zip
            return $"{DateTime.Today:yyyyMMdd}_{nextNumber}_{GetBackupBaseName(file)}.zip";
        }
    }
}
