using System;

namespace EasyBackUp
{
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
