using System;
using System.Windows;
using System.Windows.Controls;
using SshManager.Helpers;
using SshManager.Models;

namespace SshManager.Views
{
    public partial class SessionEditDialog : Window
    {
        private readonly SshSession _existingSession;

        public SessionEditDialog(int defaultPort = 22, string defaultUsername = "")
        {
            InitializeComponent();
            AuthMethodBox.SelectedIndex = 0;
            PortBox.Text = defaultPort.ToString();
            UsernameBox.Text = defaultUsername;
        }

        /// <summary>
        /// Opens the dialog pre-filled with an existing session for editing.
        /// </summary>
        public SessionEditDialog(SshSession session, int defaultPort = 22, string defaultUsername = "") : this(defaultPort, defaultUsername)
        {
            _existingSession = session;
            Title = "Edit Session";

            NameBox.Text = session.Name;
            HostBox.Text = session.Host;
            PortBox.Text = session.Port.ToString();
            UsernameBox.Text = session.Username;
            GroupBox.Text = session.Group;
            NotesBox.Text = session.Notes;
            PrivateKeyPathBox.Text = session.PrivateKeyPath;
            switch (session.AuthMethod)
            {
                case AuthMethod.PromptPassword: AuthMethodBox.SelectedIndex = 1; break;
                case AuthMethod.PrivateKey:     AuthMethodBox.SelectedIndex = 2; break;
                default:                        AuthMethodBox.SelectedIndex = 0; break;
            }

            if (!string.IsNullOrEmpty(session.EncryptedPassword))
            {
                PasswordBox.Password = PasswordHelper.Decrypt(session.EncryptedPassword);
            }
        }

        /// <summary>
        /// Builds an SshSession from the dialog fields.
        /// </summary>
        public SshSession GetSession()
        {
            int port;
            if (!int.TryParse(PortBox.Text, out port) || port < 1 || port > 65535)
                port = 22;

            return new SshSession
            {
                Id = _existingSession?.Id ?? Guid.NewGuid(),
                Name = NameBox.Text.Trim(),
                Host = HostBox.Text.Trim(),
                Port = port,
                Username = UsernameBox.Text.Trim(),
                AuthMethod = AuthMethodBox.SelectedIndex == 2
                    ? AuthMethod.PrivateKey
                    : AuthMethodBox.SelectedIndex == 1
                        ? AuthMethod.PromptPassword
                        : AuthMethod.Password,
                EncryptedPassword = PasswordHelper.Encrypt(PasswordBox.Password),
                PrivateKeyPath = PrivateKeyPathBox.Text.Trim(),
                Group = GroupBox.Text.Trim(),
                Notes = NotesBox.Text.Trim(),
                CreatedDate = _existingSession?.CreatedDate ?? DateTime.Now,
                LastConnectedDate = _existingSession?.LastConnectedDate
            };
        }

        private void AuthMethodBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PasswordPanel == null || PrivateKeyPanel == null) return;

            switch (AuthMethodBox.SelectedIndex)
            {
                case 1: // Prompt Password — no fields needed
                    PasswordPanel.Visibility = Visibility.Collapsed;
                    PrivateKeyPanel.Visibility = Visibility.Collapsed;
                    break;
                case 2: // Private Key
                    PasswordPanel.Visibility = Visibility.Collapsed;
                    PrivateKeyPanel.Visibility = Visibility.Visible;
                    break;
                default: // Saved Password
                    PasswordPanel.Visibility = Visibility.Visible;
                    PrivateKeyPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void BrowsePrivateKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Private Key File",
                Filter = "All Files (*.*)|*.*|PEM Files (*.pem)|*.pem|PPK Files (*.ppk)|*.ppk"
            };

            if (dialog.ShowDialog() == true)
            {
                PrivateKeyPathBox.Text = dialog.FileName;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Please enter a session name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(HostBox.Text))
            {
                MessageBox.Show("Please enter a host address.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                HostBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                MessageBox.Show("Please enter a username.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameBox.Focus();
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
