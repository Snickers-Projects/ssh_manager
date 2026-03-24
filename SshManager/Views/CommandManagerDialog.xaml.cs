using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SshManager.Models;

namespace SshManager.Views
{
    public partial class CommandManagerDialog : Window
    {
        private readonly List<SavedCommand> _commands;
        private readonly List<SshSession> _sessions;
        private readonly Guid? _currentSessionId;

        /// <summary>
        /// When set, the dialog was closed by running a command. Contains the command text.
        /// </summary>
        public string CommandToRun { get; private set; }

        public bool CommandsModified { get; private set; }

        public CommandManagerDialog(
            List<SavedCommand> commands,
            List<SshSession> sessions,
            Guid? currentSessionId)
        {
            InitializeComponent();

            _commands = commands;
            _sessions = sessions;
            _currentSessionId = currentSessionId;

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var search = (SearchBox.Text ?? "").Trim().ToLowerInvariant();

            // Show global commands + commands for the current session
            var filtered = _commands
                .Where(c => c.SessionId == null || c.SessionId == _currentSessionId)
                .Where(c =>
                    string.IsNullOrEmpty(search) ||
                    (c.Name ?? "").ToLowerInvariant().Contains(search) ||
                    (c.Command ?? "").ToLowerInvariant().Contains(search) ||
                    (c.Notes ?? "").ToLowerInvariant().Contains(search))
                .OrderBy(c => c.SessionId.HasValue ? 1 : 0) // Global first
                .ThenBy(c => c.Name)
                .ToList();

            CommandList.ItemsSource = filtered;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void CommandList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RunSelected();
        }

        private void RunCommand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SavedCommand cmd)
            {
                CommandToRun = cmd.Command;
                DialogResult = true;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommandEditDialog(_sessions, _currentSessionId) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _commands.Add(dialog.GetCommand());
                CommandsModified = true;
                ApplyFilter();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var selected = CommandList.SelectedItem as SavedCommand;
            if (selected == null) return;

            var dialog = new CommandEditDialog(_sessions, _currentSessionId, selected) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                var updated = dialog.GetCommand();
                var index = _commands.FindIndex(c => c.Id == selected.Id);
                if (index >= 0)
                {
                    _commands[index] = updated;
                    CommandsModified = true;
                    ApplyFilter();
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var selected = CommandList.SelectedItem as SavedCommand;
            if (selected == null) return;

            var result = MessageBox.Show(
                $"Delete command \"{selected.Name}\"?",
                "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _commands.RemoveAll(c => c.Id == selected.Id);
                CommandsModified = true;
                ApplyFilter();
            }
        }

        private void RunSelected()
        {
            var selected = CommandList.SelectedItem as SavedCommand;
            if (selected == null) return;

            CommandToRun = selected.Command;
            DialogResult = true;
        }
    }
}
