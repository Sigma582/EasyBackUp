using Microsoft.Extensions.Logging;
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
                MaxBackups = 10 },
            new TargetDefinition {
                TargetFolder = @"D:\Users\me\Documents\My Games\XCOM - Enemy Within\XComGame\SaveData\",
                Glob = "save24",
                BackupFolder = @"D:\Users\me\Documents\My Games\XCOM - Enemy Within\XComGame\SaveData\Backup\",
                Interval = TimeSpan.FromMinutes(60),
                MaxBackups = 20 },
        };

        public Worker(ILogger logger)
        {
            Logger = logger;
        }

        private int CheckIntervalSeconds => 5;

        public ILogger Logger { get; }

        public async Task ExecuteAsync(CancellationToken cxl)
        {
            await Task.WhenAll(_definitions.Select(definition => Task.Run(() => ProcessUntilCancelled(definition, cxl))));
            Logger.LogInformation($"Cancelled - ExecuteAsync");
        }

        private async Task ProcessUntilCancelled(TargetDefinition definition, CancellationToken cxl)
        {
            try
            {
                while (true)
                {
                    Process(definition, cxl);
                    await Task.Delay(CheckIntervalSeconds * 1000, cxl);
                }
            }
            catch(TaskCanceledException ex)
            {
                Logger.LogInformation("Cancelled - {Path}", Path.Combine(definition.TargetFolder, definition.Glob));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error - {Path}", Path.Combine(definition.TargetFolder, definition.Glob));
            }
        }

        private void Process(TargetDefinition definition, CancellationToken cxl)
        {
            Logger.LogInformation("processing {Path}", Path.Combine(definition.TargetFolder, definition.Glob));

            var targetDirectory = new DirectoryInfo(definition.TargetFolder);
            if (!targetDirectory.Exists)
            {
                return;
            }

            var backupDirectory = new DirectoryInfo(definition.BackupFolder);
            if (!backupDirectory.Exists)
            {
                backupDirectory.Create();
            }

            var files = targetDirectory.GetFiles(definition.Glob);

            foreach (var file in files)
            {
                //allow for early exit - don't start a new file if cancellation has been requested
                cxl.ThrowIfCancellationRequested();
                
                var existingBackups = GetExistingBackups(backupDirectory, file);
                var latestBackup = existingBackups.OrderByDescending(t => t.backupNumber).FirstOrDefault();

                //point of no return for current file
                cxl.ThrowIfCancellationRequested();

                if (latestBackup.file == null || file.LastWriteTimeUtc - latestBackup.file.LastWriteTimeUtc >= definition.Interval)
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
