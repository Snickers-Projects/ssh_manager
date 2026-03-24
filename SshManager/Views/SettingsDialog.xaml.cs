using System.Windows;
using SshManager.Models;

namespace SshManager.Views
{
    public partial class SettingsDialog : Window
    {
        private readonly AppSettings _settings;

        public SettingsDialog(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            WindowWidthBox.Text = settings.WindowWidth.ToString();
            WindowHeightBox.Text = settings.WindowHeight.ToString();
            FontSizeBox.Text = settings.TerminalFontSize.ToString();
            ScrollbackBox.Text = settings.TerminalScrollback.ToString();
            DefaultPortBox.Text = settings.DefaultPort.ToString();
            DefaultUsernameBox.Text = settings.DefaultUsername;
            TimeoutBox.Text = settings.ConnectionTimeoutSeconds.ToString();
            ConfirmCloseCheck.IsChecked = settings.ConfirmOnCloseWithActiveSessions;
        }

        public AppSettings GetSettings()
        {
            return new AppSettings
            {
                WindowWidth = ParseInt(WindowWidthBox.Text, _settings.WindowWidth),
                WindowHeight = ParseInt(WindowHeightBox.Text, _settings.WindowHeight),
                TerminalFontSize = ParseInt(FontSizeBox.Text, _settings.TerminalFontSize),
                TerminalScrollback = ParseInt(ScrollbackBox.Text, _settings.TerminalScrollback),
                DefaultPort = ParseInt(DefaultPortBox.Text, _settings.DefaultPort),
                DefaultUsername = DefaultUsernameBox.Text.Trim(),
                ConnectionTimeoutSeconds = ParseInt(TimeoutBox.Text, _settings.ConnectionTimeoutSeconds),
                ConfirmOnCloseWithActiveSessions = ConfirmCloseCheck.IsChecked == true
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = GetSettings();

            if (settings.WindowWidth < 400 || settings.WindowHeight < 300)
            {
                MessageBox.Show("Window size must be at least 400 x 300.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (settings.TerminalFontSize < 6 || settings.TerminalFontSize > 72)
            {
                MessageBox.Show("Font size must be between 6 and 72.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (settings.DefaultPort < 1 || settings.DefaultPort > 65535)
            {
                MessageBox.Show("Port must be between 1 and 65535.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (settings.ConnectionTimeoutSeconds < 1 || settings.ConnectionTimeoutSeconds > 300)
            {
                MessageBox.Show("Timeout must be between 1 and 300 seconds.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private static int ParseInt(string text, int fallback)
        {
            return int.TryParse(text.Trim(), out var val) ? val : fallback;
        }
    }
}
