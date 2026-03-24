using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Renci.SshNet;
using SshManager.Models;
using SshManager.Services;

namespace SshManager.ViewModels
{
    /// <summary>
    /// ViewModel for a single open SSH terminal tab.
    /// Manages the SSH connection, shell stream, and terminal I/O.
    /// The View (TerminalTabView) handles rendering via WebView2/xterm.js.
    /// </summary>
    public class TerminalTabViewModel : BaseViewModel, IDisposable
    {
        private readonly ISshConnectionService _connectionService;
        private SshClient _client;
        private ShellStream _shellStream;

        public SshSession Session { get; }
        public string TabTitle => Session.Name;

        /// <summary>
        /// Raised on the UI thread when raw bytes arrive from the SSH server.
        /// The View subscribes to this and forwards data to xterm.js.
        /// </summary>
        public event Action<byte[]> SshDataReceived;

        /// <summary>
        /// Raised on the UI thread for status/error messages (plain text).
        /// </summary>
        public event Action<string> StatusMessageReceived;

        /// <summary>
        /// Raised when this tab is disposed. The View uses this to clean up WebView2.
        /// </summary>
        public event Action Disposed;

        private string _statusText = "Disconnected";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public ICommand DisconnectCommand { get; }
        public ICommand ReconnectCommand { get; }

        public TerminalTabViewModel(SshSession session, ISshConnectionService connectionService)
        {
            Session = session;
            _connectionService = connectionService;

            DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);
            ReconnectCommand = new RelayCommand(
                async () => await ConnectAsync(),
                () => !IsConnected);
        }

        public async Task ConnectAsync(string plaintextPassword = null)
        {
            StatusText = "Connecting...";
            RaiseStatusMessage($"*** Connecting to {Session.Host}:{Session.Port} as {Session.Username} ***\r\n");

            try
            {
                await Task.Run(() =>
                {
                    _client = plaintextPassword != null
                        ? _connectionService.Connect(Session, plaintextPassword)
                        : _connectionService.Connect(Session);
                    _shellStream = _connectionService.CreateShellStream(_client);
                });

                _shellStream.DataReceived += ShellStream_DataReceived;
                _client.ErrorOccurred += Client_ErrorOccurred;

                IsConnected = true;
                StatusText = $"Connected to {Session.Host}";
            }
            catch (Exception ex)
            {
                StatusText = "Connection failed";
                RaiseStatusMessage($"*** Connection failed: {ex.Message} ***\r\n");
            }
        }

        /// <summary>
        /// Sends raw user input (from xterm.js) to the SSH shell.
        /// </summary>
        public void SendInput(string data)
        {
            if (_shellStream == null || !IsConnected || string.IsNullOrEmpty(data))
                return;

            var bytes = Encoding.UTF8.GetBytes(data);
            _shellStream.Write(bytes, 0, bytes.Length);
            _shellStream.Flush();
        }

        /// <summary>
        /// Notifies the SSH server that the terminal was resized.
        /// Uses reflection to access the internal channel on ShellStream.
        /// </summary>
        public void Resize(uint cols, uint rows)
        {
            if (_shellStream == null || !IsConnected) return;

            try
            {
                var channelField = typeof(ShellStream).GetField("_channel",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (channelField == null) return;

                var channel = channelField.GetValue(_shellStream);
                if (channel == null) return;

                var method = channel.GetType().GetMethod("SendWindowChangeRequest");
                if (method == null) return;

                method.Invoke(channel, new object[] { cols, rows, (uint)0, (uint)0 });
            }
            catch { }
        }

        private void ShellStream_DataReceived(object sender, Renci.SshNet.Common.ShellDataEventArgs e)
        {
            var data = e.Data;
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                SshDataReceived?.Invoke(data);
            }));
        }

        private void Client_ErrorOccurred(object sender, Renci.SshNet.Common.ExceptionEventArgs e)
        {
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                RaiseStatusMessage($"\r\n*** Error: {e.Exception.Message} ***\r\n");
                IsConnected = false;
                StatusText = "Disconnected (error)";
            }));
        }

        private void RaiseStatusMessage(string message)
        {
            StatusMessageReceived?.Invoke(message);
        }

        public void Disconnect()
        {
            try
            {
                if (_shellStream != null)
                {
                    _shellStream.DataReceived -= ShellStream_DataReceived;
                    _shellStream.Dispose();
                    _shellStream = null;
                }

                if (_client != null)
                {
                    _client.ErrorOccurred -= Client_ErrorOccurred;
                    if (_client.IsConnected)
                        _client.Disconnect();
                    _client.Dispose();
                    _client = null;
                }
            }
            catch { }

            IsConnected = false;
            StatusText = "Disconnected";
            RaiseStatusMessage("\r\n*** Disconnected ***\r\n");
        }

        public void Dispose()
        {
            try
            {
                if (_shellStream != null)
                {
                    _shellStream.DataReceived -= ShellStream_DataReceived;
                    _shellStream.Dispose();
                    _shellStream = null;
                }

                if (_client != null)
                {
                    _client.ErrorOccurred -= Client_ErrorOccurred;
                    if (_client.IsConnected)
                        _client.Disconnect();
                    _client.Dispose();
                    _client = null;
                }
            }
            catch { }

            IsConnected = false;
            Disposed?.Invoke();
        }
    }
}
