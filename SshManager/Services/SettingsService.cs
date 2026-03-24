using System.IO;
using Newtonsoft.Json;
using SshManager.Helpers;
using SshManager.Models;

namespace SshManager.Services
{
    /// <summary>
    /// Persists application settings as JSON in the portable data folder.
    /// </summary>
    public class SettingsService
    {
        private readonly string _filePath;
        private AppSettings _current;

        public SettingsService()
        {
            _filePath = Path.Combine(AppPaths.DataFolder, "settings.json");
        }

        public AppSettings Load()
        {
            if (_current != null)
                return _current;

            if (!File.Exists(_filePath))
            {
                _current = new AppSettings();
                return _current;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                _current = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                _current = new AppSettings();
            }
            return _current;
        }

        public void Save(AppSettings settings)
        {
            _current = settings;
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }
    }
}
