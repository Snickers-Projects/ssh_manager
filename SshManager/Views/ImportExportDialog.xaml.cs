using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SshManager.Models;
using SshManager.Services;

namespace SshManager.Views
{
    public partial class ImportExportDialog : Window
    {
        private readonly List<IImportExportProvider> _providers;
        private readonly List<SshSession> _sessions;
        private readonly List<SavedCommand> _commands;

        /// <summary>
        /// After the dialog closes, check this to see if any data was imported.
        /// </summary>
        public bool DataImported { get; private set; }
        public List<SshSession> ImportedSessions { get; private set; }
        public List<SavedCommand> ImportedCommands { get; private set; }

        public ImportExportDialog(
            List<IImportExportProvider> providers,
            List<SshSession> sessions,
            List<SavedCommand> commands)
        {
            InitializeComponent();

            _providers = providers;
            _sessions = sessions;
            _commands = commands;

            var exportProviders = providers.Where(p => p.CanExport).ToList();
            var importProviders = providers.Where(p => p.CanImport).ToList();

            ExportFormatCombo.ItemsSource = exportProviders;
            if (exportProviders.Count > 0)
                ExportFormatCombo.SelectedIndex = 0;

            ImportFormatCombo.ItemsSource = importProviders;
            if (importProviders.Count > 0)
                ImportFormatCombo.SelectedIndex = 0;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var provider = ExportFormatCombo.SelectedItem as IImportExportProvider;
            if (provider == null) return;

            var dlg = new SaveFileDialog
            {
                Title = "Export Sessions",
                Filter = provider.FileFilter,
                FileName = "ssh_manager_export"
            };

            if (dlg.ShowDialog(this) != true) return;

            try
            {
                var data = new ExportData
                {
                    Sessions = _sessions,
                    Commands = _commands
                };
                provider.Export(dlg.FileName, data);
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = $"Exported {_sessions.Count} sessions, {_commands.Count} commands.";
            }
            catch (Exception ex)
            {
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                StatusText.Text = $"Export failed: {ex.Message}";
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var provider = ImportFormatCombo.SelectedItem as IImportExportProvider;
            if (provider == null) return;

            var dlg = new OpenFileDialog
            {
                Title = "Import Sessions",
                Filter = provider.FileFilter
            };

            if (dlg.ShowDialog(this) != true) return;

            try
            {
                var data = provider.Import(dlg.FileName);

                // Deduplicate sessions by Name + Host
                var existingKeys = new HashSet<string>(
                    _sessions.Select(s => $"{s.Name}|{s.Host}".ToLowerInvariant()));

                var newSessions = (data.Sessions ?? new List<SshSession>())
                    .Where(s => !existingKeys.Contains($"{s.Name}|{s.Host}".ToLowerInvariant()))
                    .ToList();

                // Assign new Ids to imported sessions to avoid collisions
                foreach (var s in newSessions)
                    s.Id = Guid.NewGuid();

                // Deduplicate commands by Name + Command text
                var existingCmdKeys = new HashSet<string>(
                    _commands.Select(c => $"{c.Name}|{c.Command}".ToLowerInvariant()));

                var newCommands = (data.Commands ?? new List<SavedCommand>())
                    .Where(c => !existingCmdKeys.Contains($"{c.Name}|{c.Command}".ToLowerInvariant()))
                    .ToList();

                foreach (var c in newCommands)
                    c.Id = Guid.NewGuid();

                ImportedSessions = newSessions;
                ImportedCommands = newCommands;
                DataImported = newSessions.Count > 0 || newCommands.Count > 0;

                int skippedSessions = (data.Sessions?.Count ?? 0) - newSessions.Count;
                int skippedCommands = (data.Commands?.Count ?? 0) - newCommands.Count;

                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = $"Imported {newSessions.Count} sessions, {newCommands.Count} commands.";
                if (skippedSessions > 0 || skippedCommands > 0)
                    StatusText.Text += $" Skipped {skippedSessions} duplicate sessions, {skippedCommands} duplicate commands.";
            }
            catch (Exception ex)
            {
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                StatusText.Text = $"Import failed: {ex.Message}";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = DataImported;
        }
    }
}
