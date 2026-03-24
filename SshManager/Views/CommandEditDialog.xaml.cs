using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SshManager.Models;

namespace SshManager.Views
{
    public partial class CommandEditDialog : Window
    {
        private readonly List<SshSession> _sessions;
        private readonly Guid? _currentSessionId;
        private readonly SavedCommand _existing;

        // Items for scope combo: (display, sessionId)
        private List<ScopeItem> _scopeItems;

        public CommandEditDialog(
            List<SshSession> sessions,
            Guid? currentSessionId,
            SavedCommand existing = null)
        {
            InitializeComponent();

            _sessions = sessions;
            _currentSessionId = currentSessionId;
            _existing = existing;

            // Build scope options
            _scopeItems = new List<ScopeItem>
            {
                new ScopeItem("All servers (global)", null)
            };

            // If there's a current session, offer it as the first server-specific option
            if (currentSessionId.HasValue)
            {
                var currentSession = sessions.FirstOrDefault(s => s.Id == currentSessionId.Value);
                if (currentSession != null)
                    _scopeItems.Add(new ScopeItem($"Current server ({currentSession.Name})", currentSession.Id));
            }

            // Add all other sessions
            foreach (var s in sessions.OrderBy(s => s.Name))
            {
                if (s.Id != currentSessionId)
                    _scopeItems.Add(new ScopeItem(s.Name, s.Id));
            }

            ScopeCombo.ItemsSource = _scopeItems;
            ScopeCombo.DisplayMemberPath = "Display";
            ScopeCombo.SelectedIndex = 0;

            if (existing != null)
            {
                Title = "Edit Command";
                NameBox.Text = existing.Name;
                CommandBox.Text = existing.Command;
                NotesBox.Text = existing.Notes;

                // Select matching scope
                var matchIndex = _scopeItems.FindIndex(s => s.SessionId == existing.SessionId);
                if (matchIndex >= 0)
                    ScopeCombo.SelectedIndex = matchIndex;
            }
        }

        public SavedCommand GetCommand()
        {
            var scope = ScopeCombo.SelectedItem as ScopeItem;
            return new SavedCommand
            {
                Id = _existing?.Id ?? Guid.NewGuid(),
                Name = NameBox.Text.Trim(),
                Command = CommandBox.Text.Trim(),
                Notes = NotesBox.Text.Trim(),
                SessionId = scope?.SessionId,
                CreatedDate = _existing?.CreatedDate ?? DateTime.Now
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameBox.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(CommandBox.Text))
            {
                MessageBox.Show("Command is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                CommandBox.Focus();
                return;
            }

            DialogResult = true;
        }

        private class ScopeItem
        {
            public string Display { get; }
            public Guid? SessionId { get; }

            public ScopeItem(string display, Guid? sessionId)
            {
                Display = display;
                SessionId = sessionId;
            }
        }
    }
}
