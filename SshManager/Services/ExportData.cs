using System.Collections.Generic;
using SshManager.Models;

namespace SshManager.Services
{
    /// <summary>
    /// Encapsulates all exportable application data.
    /// Extend this class to include new data types (e.g., commands) as features grow.
    /// </summary>
    public class ExportData
    {
        public List<SshSession> Sessions { get; set; } = new List<SshSession>();
        public List<SavedCommand> Commands { get; set; } = new List<SavedCommand>();
    }
}
