using System;
using System.Collections.Generic;
using System.IO;
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

        private void Process(TargetDefinition definition)
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
                //MyFile.docx_bak
                var backupBaseName = $"{file.Name}_bak";

                var existingBackups = GetExistingBackups(backupDirectory, backupBaseName);

                var nextNumber = existingBackups.Any() ? existingBackups.Max(t => t.backupNumber) + 1 : 1;

                //20220215_59_MyFile.docx_bak.zip
                var newBackupName = $"{DateTime.Today:yyyyMMdd}_{nextNumber}_{backupBaseName}.zip";

                //todo zip the target file
                File.Create(Path.Combine(backupDirectory.FullName, newBackupName)).Dispose();

                if (existingBackups.Count + 1 > definition.MaxBackups)
                {
                    foreach (var item in existingBackups.OrderByDescending(kvp => kvp.backupNumber).Skip(definition.MaxBackups - 1))
                    {
                        item.file.Delete();
                    }
                }
            }
        }

        private static ICollection<(int backupNumber, FileInfo file)> GetExistingBackups(DirectoryInfo backupDirectory, string backupBaseName)
        {
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
    }

    public class TargetDefinition
    {
        /// <summary>
        /// Folder to backup files from.
        /// </summary>
        public string TargetFolder { get; set; }

        /// <summary>
        /// Filter to select target files in the <see cref="TargetFolder"/>.
        /// </summary>
        public string Glob { get; set; }

        /// <summary>
        /// Folder to put backup copies into.
        /// </summary>
        public string BackupFolder{ get; set; }

        /// <summary>
        /// Interval between subsequent backup passes. A modified file will not be backed up if there is already a backup of the same file created less than <see cref="Interval" ago/>
        /// </summary>
        public TimeSpan Interval{ get; set; }

        /// <summary>
        /// Maximum number of backups of each file to be kept.
        /// </summary>
        public int MaxBackups { get; set; }
    }
}
