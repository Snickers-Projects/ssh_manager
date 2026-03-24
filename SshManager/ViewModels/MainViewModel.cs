using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using SshManager.Helpers;
using SshManager.Models;
using SshManager.Services;

namespace SshManager.ViewModels
{
    /// <summary>
    /// Main application ViewModel. Manages the session list, search filtering, and open tabs.
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly ISessionStorageService _storageService;
        private readonly ISshConnectionService _sshService;
        private List<SshSession> _allSessions;
        private List<SavedCommand> _allCommands;

        // -- Session list --

        private ObservableCollection<SshSession> _filteredSessions;
        public ObservableCollection<SshSession> FilteredSessions
        {
            get => _filteredSessions;
            set => SetProperty(ref _filteredSessions, value);
        }

        private SshSession _selectedSession;
        public SshSession SelectedSession
        {
            get => _selectedSession;
            set => SetProperty(ref _selectedSession, value);
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyFilter();
            }
        }

        // -- Tabs --

        public ObservableCollection<TerminalTabViewModel> OpenTabs { get; }
            = new ObservableCollection<TerminalTabViewModel>();

        private TerminalTabViewModel _selectedTab;
        public TerminalTabViewModel SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

        // -- Commands --

        public ICommand ConnectCommand { get; }
        public ICommand OpenInPowerShellCommand { get; }
        public ICommand DeleteSessionCommand { get; }
        public ICommand CloseTabCommand { get; }

        /// <summary>
        /// Set by the View. Returns the entered password, or null if cancelled.
        /// Parameters: sessionName, host.
        /// </summary>
        public Func<string, string, string> PasswordPromptFunc { get; set; }

        // -- Public access to data (for dialogs) --

        public List<SshSession> AllSessions => _allSessions;
        public List<SavedCommand> AllCommands => _allCommands;

        // -- Constructor --

        public MainViewModel(ISessionStorageService storageService, ISshConnectionService sshService)
        {
            _storageService = storageService;
            _sshService = sshService;

            _allSessions = _storageService.LoadSessions();
            _allCommands = _storageService.LoadCommands();
            _filteredSessions = new ObservableCollection<SshSession>(_allSessions);

            ConnectCommand = new RelayCommand(ExecuteConnect, () => SelectedSession != null);
            OpenInPowerShellCommand = new RelayCommand(ExecuteOpenInPowerShell, () => SelectedSession != null);
            DeleteSessionCommand = new RelayCommand(ExecuteDeleteSession, () => SelectedSession != null);
            CloseTabCommand = new RelayCommand(ExecuteCloseTab);
        }

        // -- Search / Filter --

        private void ApplyFilter()
        {
            var search = (SearchText ?? "").Trim().ToLowerInvariant();

            var filtered = string.IsNullOrEmpty(search)
                ? _allSessions
                : _allSessions.Where(s =>
                    (s.Name ?? "").ToLowerInvariant().Contains(search) ||
                    (s.Host ?? "").ToLowerInvariant().Contains(search) ||
                    (s.Username ?? "").ToLowerInvariant().Contains(search) ||
                    (s.Group ?? "").ToLowerInvariant().Contains(search) ||
                    (s.Notes ?? "").ToLowerInvariant().Contains(search));

            FilteredSessions = new ObservableCollection<SshSession>(filtered);
        }

        // -- Session CRUD (called by View code-behind for dialogs) --

        public async void ExecuteConnect()
        {
            if (SelectedSession == null) return;

            var session = SelectedSession;
            string promptedPassword = null;

            if (session.AuthMethod == AuthMethod.PromptPassword)
            {
                promptedPassword = PasswordPromptFunc?.Invoke(session.Name, session.Host);
                if (promptedPassword == null)
                    return; // User cancelled
            }

            session.LastConnectedDate = DateTime.Now;
            SaveSessions();

            var tab = new TerminalTabViewModel(session, _sshService);
            OpenTabs.Add(tab);
            SelectedTab = tab;
            await tab.ConnectAsync(promptedPassword);
        }

        public void AddSession(SshSession session)
        {
            _allSessions.Add(session);
            SaveSessions();
            ApplyFilter();
        }

        public void UpdateSession(SshSession original, SshSession updated)
        {
            var index = _allSessions.FindIndex(s => s.Id == original.Id);
            if (index >= 0)
            {
                _allSessions[index] = updated;
                SaveSessions();
                ApplyFilter();
            }
        }

        public void ExecuteOpenInPowerShell()
        {
            if (SelectedSession == null) return;

            var session = SelectedSession;
            session.LastConnectedDate = DateTime.Now;
            SaveSessions();

            // Build ssh command with arguments
            var args = $"-NoExit -Command \"ssh {session.Username}@{session.Host} -p {session.Port}";
            if (session.AuthMethod == AuthMethod.PrivateKey && !string.IsNullOrWhiteSpace(session.PrivateKeyPath))
            {
                args += $" -i \\\"{session.PrivateKeyPath}\\\"";
            }
            args += "\"";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = args,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to open PowerShell: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteSession()
        {
            if (SelectedSession == null) return;

            _allSessions.RemoveAll(s => s.Id == SelectedSession.Id);
            SaveSessions();
            ApplyFilter();
        }

        private void ExecuteCloseTab(object parameter)
        {
            if (parameter is TerminalTabViewModel tab)
            {
                tab.Dispose();
                OpenTabs.Remove(tab);
            }
        }

        private void SaveSessions()
        {
            _storageService.SaveSessions(_allSessions);
        }

        public void SaveCommands()
        {
            _storageService.SaveCommands(_allCommands);
        }

        // -- Import / Export --

        public void ApplyImportedData(List<SshSession> sessions, List<SavedCommand> commands)
        {
            if (sessions != null && sessions.Count > 0)
            {
                _allSessions.AddRange(sessions);
                SaveSessions();
                ApplyFilter();
            }

            if (commands != null && commands.Count > 0)
            {
                _allCommands.AddRange(commands);
                SaveCommands();
            }
        }

        // -- Commands --

        public void SendCommandToActiveTab(string commandText)
        {
            if (SelectedTab == null || !SelectedTab.IsConnected) return;
            SelectedTab.SendInput(commandText + "\n");
        }
    }
}
