using System.Collections.Generic;
using SshManager.Models;

namespace SshManager.Services
{
    /// <summary>
    /// Persists SSH sessions and saved commands to storage.
    /// </summary>
    public interface ISessionStorageService
    {
        List<SshSession> LoadSessions();
        void SaveSessions(List<SshSession> sessions);

        List<SavedCommand> LoadCommands();
        void SaveCommands(List<SavedCommand> commands);
    }
}
