using System;

namespace RavendbBackup
{
    public class RavendbBackupStatus
    {
        public bool IsRunning { get; set; }
        public DateTime? Started { get; set; }
        public DateTime? Completed { get; set; }
    }
}