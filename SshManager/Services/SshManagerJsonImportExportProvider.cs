using System.IO;
using Newtonsoft.Json;

namespace SshManager.Services
{
    /// <summary>
    /// Import/export provider for SSH Manager's native JSON format.
    /// Exports sessions and commands into a single JSON file.
    /// </summary>
    public class SshManagerJsonImportExportProvider : IImportExportProvider
    {
        public string DisplayName => "SSH Manager (JSON)";
        public string FileFilter => "JSON files (*.json)|*.json";
        public bool CanExport => true;
        public bool CanImport => true;

        public ExportData Import(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<ExportData>(json) ?? new ExportData();
        }

        public void Export(string filePath, ExportData data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
