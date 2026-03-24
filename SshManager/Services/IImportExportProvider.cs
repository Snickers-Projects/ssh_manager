namespace SshManager.Services
{
    /// <summary>
    /// Provides sessions (and optionally commands) for import.
    /// Each import format implements this interface.
    /// For example: SshManagerJsonImportExportProvider, PuttyImportProvider, etc.
    /// </summary>
    public interface IImportExportProvider
    {
        /// <summary>Display name shown in the UI (e.g. "SSH Manager (JSON)").</summary>
        string DisplayName { get; }

        /// <summary>File dialog filter (e.g. "JSON files (*.json)|*.json").</summary>
        string FileFilter { get; }

        /// <summary>Whether this provider supports exporting.</summary>
        bool CanExport { get; }

        /// <summary>Whether this provider supports importing.</summary>
        bool CanImport { get; }

        /// <summary>Import data from a file.</summary>
        ExportData Import(string filePath);

        /// <summary>Export data to a file.</summary>
        void Export(string filePath, ExportData data);
    }
}
