using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SshManager.Helpers;
using SshManager.Models;

namespace SshManager.Services
{
    /// <summary>
    /// Stores sessions and commands as JSON files in the portable data folder.
    /// </summary>
    public class JsonSessionStorageService : ISessionStorageService
    {
        private readonly string _sessionsPath;
        private readonly string _commandsPath;

        public JsonSessionStorageService()
        {
            _sessionsPath = Path.Combine(AppPaths.DataFolder, "sessions.json");
            _commandsPath = Path.Combine(AppPaths.DataFolder, "commands.json");
        }

        public List<SshSession> LoadSessions()
        {
            if (!File.Exists(_sessionsPath))
                return new List<SshSession>();

            var json = File.ReadAllText(_sessionsPath);
            return JsonConvert.DeserializeObject<List<SshSession>>(json) ?? new List<SshSession>();
        }

        public void SaveSessions(List<SshSession> sessions)
        {
            var json = JsonConvert.SerializeObject(sessions, Formatting.Indented);
            File.WriteAllText(_sessionsPath, json);
        }

        public List<SavedCommand> LoadCommands()
        {
            if (!File.Exists(_commandsPath))
                return new List<SavedCommand>();

            var json = File.ReadAllText(_commandsPath);
            return JsonConvert.DeserializeObject<List<SavedCommand>>(json) ?? new List<SavedCommand>();
        }

        public void SaveCommands(List<SavedCommand> commands)
        {
            var json = JsonConvert.SerializeObject(commands, Formatting.Indented);
            File.WriteAllText(_commandsPath, json);
        }
    }
}
