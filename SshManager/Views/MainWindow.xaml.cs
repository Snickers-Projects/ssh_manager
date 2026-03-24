using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SshManager.Models;
using SshManager.Services;
using SshManager.ViewModels;

namespace SshManager.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;
        private readonly SettingsService _settingsService = new SettingsService();
        private AppSettings _settings;

        public MainWindow()
        {
            InitializeComponent();

            _settings = _settingsService.Load();

            // Apply saved window size
            Width = _settings.WindowWidth;
            Height = _settings.WindowHeight;

            var storage = new JsonSessionStorageService();
            var sshService = new SshConnectionService
            {
                ConnectionTimeoutSeconds = _settings.ConnectionTimeoutSeconds
            };
            DataContext = new MainViewModel(storage, sshService);

            ViewModel.PasswordPromptFunc = (name, host) =>
            {
                var dlg = new PasswordPromptDialog(name, host) { Owner = this };
                return dlg.ShowDialog() == true ? dlg.Password : null;
            };
        }

        private void AddSession_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SessionEditDialog(_settings.DefaultPort, _settings.DefaultUsername) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                ViewModel.AddSession(dialog.GetSession());
            }
        }

        private void EditSession_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedSession == null) return;

            var dialog = new SessionEditDialog(ViewModel.SelectedSession, _settings.DefaultPort, _settings.DefaultUsername) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                ViewModel.UpdateSession(ViewModel.SelectedSession, dialog.GetSession());
            }
        }

        private void SessionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedSession != null)
            {
                ViewModel.ExecuteConnect();
            }
        }

        private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed && sender is System.Windows.Controls.TabItem tabItem)
            {
                if (tabItem.DataContext is TerminalTabViewModel tab)
                {
                    ViewModel.CloseTabCommand.Execute(tab);
                    e.Handled = true;
                }
            }
        }

        private void ImportExport_Click(object sender, RoutedEventArgs e)
        {
            var providers = new List<IImportExportProvider>
            {
                new SshManagerJsonImportExportProvider()
                // Future: new PuttyImportProvider(), etc.
            };

            var dialog = new ImportExportDialog(
                providers,
                ViewModel.AllSessions,
                ViewModel.AllCommands)
            { Owner = this };

            if (dialog.ShowDialog() == true && dialog.DataImported)
            {
                ViewModel.ApplyImportedData(dialog.ImportedSessions, dialog.ImportedCommands);
            }
        }

        private void Commands_Click(object sender, RoutedEventArgs e)
        {
            var currentSessionId = ViewModel.SelectedTab?.Session?.Id;

            var dialog = new CommandManagerDialog(
                ViewModel.AllCommands,
                ViewModel.AllSessions,
                currentSessionId)
            { Owner = this };

            var result = dialog.ShowDialog();

            // Save if commands were modified (add/edit/delete)
            if (dialog.CommandsModified)
                ViewModel.SaveCommands();

            // If a command was selected to run, send it to the active tab
            if (result == true && dialog.CommandToRun != null)
            {
                ViewModel.SendCommandToActiveTab(dialog.CommandToRun);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog(_settings) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _settings = dialog.GetSettings();
                _settingsService.Save(_settings);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_settings.ConfirmOnCloseWithActiveSessions &&
                ViewModel.OpenTabs.Any(t => t.IsConnected))
            {
                var result = MessageBox.Show(
                    "There are active SSH connections. Close anyway?",
                    "Confirm Close",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            foreach (var tab in ViewModel.OpenTabs)
            {
                tab.Dispose();
            }
            base.OnClosing(e);
        }
    }
}
