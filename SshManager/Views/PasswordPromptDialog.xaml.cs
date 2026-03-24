using System.Windows;
using System.Windows.Input;

namespace SshManager.Views
{
    public partial class PasswordPromptDialog : Window
    {
        public string Password => PasswordInput.Password;

        public PasswordPromptDialog(string sessionName, string host)
        {
            InitializeComponent();
            PromptText.Text = $"Enter password for {sessionName} ({host}):";
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = true;
                e.Handled = true;
            }
        }
    }
}
