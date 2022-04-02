using Microsoft.Extensions.Hosting;
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
    public class BackupService : BackgroundService
    {
        private List<TargetDefinition> Definitions { get; }

        public BackupService(ILogger<BackupService> logger, Microsoft.Extensions.Options.IOptions<List<TargetDefinition>> options)
        {
            Logger = logger;
            Definitions = options.Value;
        }

        private int CheckIntervalSeconds => 5;

        public ILogger Logger { get; }

        protected override async Task ExecuteAsync(CancellationToken cxl)
        {
            await Task.WhenAll(Definitions.Select(definition => Task.Run(() => ProcessUntilCancelled(definition, cxl))));
            Logger.LogDebug($"Cancelled - ExecuteAsync");
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
                Logger.LogDebug("Cancelled - {Path}", Path.Combine(definition.TargetFolder, definition.Glob));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error - {Path}", Path.Combine(definition.TargetFolder, definition.Glob));
            }
        }

        private void Process(TargetDefinition definition, CancellationToken cxl)
        {
            Logger.LogDebug("Processing '{Path}'", Path.Combine(definition.TargetFolder, definition.Glob));

            var targetDirectory = new DirectoryInfo(definition.TargetFolder);
            if (!targetDirectory.Exists)
            {
                return;
            }

            var backupDirectory = new DirectoryInfo(definition.BackupFolder);
            if (!backupDirectory.Exists)
            {
                backupDirectory.Create();
                Logger.LogDebug("Created backup directory '{BackupDirectory}'", backupDirectory.FullName);
            }

            var files = targetDirectory.GetFiles(definition.Glob);
            Logger.LogDebug("{Count} files discovered", files.Length);

            foreach (var file in files)
            {
                Logger.LogDebug("Processing file '{File}'", file.FullName);

                //allow for early exit - don't start a new file if cancellation has been requested
                cxl.ThrowIfCancellationRequested();
                
                var existingBackups = GetExistingBackups(backupDirectory, file);
                var latestBackup = existingBackups.OrderByDescending(t => t.backupNumber).FirstOrDefault();

                Logger.LogDebug("Latest modification for file '{File}' is as of {Timestamp}", file.FullName, file.LastWriteTime);
                if (latestBackup == default)
                {
                    Logger.LogDebug("No backups found for file '{File}'", file.FullName);
                }
                else
                {
                    Logger.LogDebug("Latest backup for file '{File}' is as of {Timestamp}", file.FullName, latestBackup.file.LastWriteTime);
                }

                //point of no return for current file
                cxl.ThrowIfCancellationRequested();

                if (latestBackup.file == null || file.LastWriteTimeUtc - latestBackup.file.LastWriteTimeUtc >= definition.Interval)
                {
                    Archive(file, backupDirectory, latestBackup.backupNumber + 1);
                    existingBackups = GetExistingBackups(backupDirectory, file);
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

        private void Archive(FileInfo file, DirectoryInfo backupDirectory, int nextNumber)
        {
            var newBackupName = GetNextBackupName(file, nextNumber);

            using var stream = new FileStream(Path.Combine(backupDirectory.FullName, newBackupName), FileMode.Create);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(file.FullName, file.Name, CompressionLevel.Fastest);

            Logger.LogInformation("Created backup '{BackupName}' for file '{File}'", newBackupName, file.FullName);
        }

        private void Prune(ICollection<(int backupNumber, FileInfo file)> existingBackups, int maxBackups)
        {
            if (existingBackups.Count + 1 > maxBackups)
            {
                foreach (var item in existingBackups.OrderByDescending(kvp => kvp.backupNumber).Skip(maxBackups - 1))
                {
                    item.file.Delete();
                    Logger.LogInformation("Deleted old backup '{BackupName}'", item.file.FullName);
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
