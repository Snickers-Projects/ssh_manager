using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using SshManager.Helpers;
using SshManager.ViewModels;

namespace SshManager.Views
{
    public partial class TerminalTabView : UserControl
    {
        private TerminalTabViewModel _vm;
        private bool _webViewReady;
        private readonly Queue<string> _pendingScripts = new Queue<string>();

        private static string _cachedHtml;
        private static int _cachedFontSize;
        private static int _cachedScrollback;

        public TerminalTabView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old VM
            if (e.OldValue is TerminalTabViewModel oldVm)
            {
                oldVm.SshDataReceived -= OnSshDataReceived;
                oldVm.StatusMessageReceived -= OnStatusMessage;
                oldVm.Disposed -= OnVmDisposed;
            }

            // Subscribe to new VM immediately so we never miss data
            if (e.NewValue is TerminalTabViewModel newVm)
            {
                _vm = newVm;
                _vm.SshDataReceived += OnSshDataReceived;
                _vm.StatusMessageReceived += OnStatusMessage;
                _vm.Disposed += OnVmDisposed;
            }
        }

        private void OnVmDisposed()
        {
            // Unsubscribe all VM events
            if (_vm != null)
            {
                _vm.SshDataReceived -= OnSshDataReceived;
                _vm.StatusMessageReceived -= OnStatusMessage;
                _vm.Disposed -= OnVmDisposed;
            }

            // Unsubscribe WebView2 events and dispose
            _webViewReady = false;
            if (TerminalWebView.CoreWebView2 != null)
            {
                TerminalWebView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            }
            TerminalWebView.Dispose();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_vm == null)
            {
                _vm = DataContext as TerminalTabViewModel;
                if (_vm != null)
                {
                    _vm.SshDataReceived += OnSshDataReceived;
                    _vm.StatusMessageReceived += OnStatusMessage;
                    _vm.Disposed += OnVmDisposed;
                }
            }
            if (_vm == null) return;

            // Initialize WebView2 with shared user data folder
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(
                    null, Helpers.AppPaths.WebView2UserDataFolder, null);
                await TerminalWebView.EnsureCoreWebView2Async(env);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "WebView2 runtime not found. It is required for the terminal.\n\n" +
                    "WebView2 is pre-installed on Windows 10/11. If missing, download from:\n" +
                    "https://developer.microsoft.com/en-us/microsoft-edge/webview2/\n\n" +
                    $"Error: {ex.Message}",
                    "WebView2 Required",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Receive messages from xterm.js
            TerminalWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            // Load the terminal HTML (cached across tabs, invalidated on settings change)
            var settings = new Services.SettingsService().Load();
            if (_cachedHtml == null ||
                _cachedFontSize != settings.TerminalFontSize ||
                _cachedScrollback != settings.TerminalScrollback)
            {
                _cachedFontSize = settings.TerminalFontSize;
                _cachedScrollback = settings.TerminalScrollback;
                _cachedHtml = TerminalHtmlBuilder.Build(settings.TerminalFontSize, settings.TerminalScrollback);
            }

            TerminalWebView.NavigateToString(_cachedHtml);
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // postMessage sends a string, so use TryGetWebMessageAsString
            string raw;
            try
            {
                raw = e.TryGetWebMessageAsString();
            }
            catch
            {
                // Fallback: strip outer quotes from WebMessageAsJson
                raw = e.WebMessageAsJson?.Trim('"');
                if (raw != null)
                    raw = raw.Replace("\\\"", "\"").Replace("\\\\", "\\");
            }

            if (string.IsNullOrEmpty(raw)) return;

            JObject json;
            try { json = JObject.Parse(raw); }
            catch { return; }

            var type = json["type"]?.ToString();

            switch (type)
            {
                case "ready":
                    _webViewReady = true;
                    FlushPendingScripts();
                    break;

                case "input":
                    var data = json["data"]?.ToString();
                    _vm?.SendInput(data);
                    break;

                case "resize":
                    var cols = json["cols"]?.Value<uint>() ?? 0;
                    var rows = json["rows"]?.Value<uint>() ?? 0;
                    if (cols > 0 && rows > 0)
                        _vm?.Resize(cols, rows);
                    break;
            }
        }

        private void OnSshDataReceived(byte[] data)
        {
            var base64 = Convert.ToBase64String(data);
            var script = $"writeData('{base64}')";

            if (_webViewReady && TerminalWebView.CoreWebView2 != null)
            {
                TerminalWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            else
            {
                _pendingScripts.Enqueue(script);
            }
        }

        private void OnStatusMessage(string message)
        {
            var escaped = message
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");

            var script = $"writeStatus('{escaped}')";

            if (_webViewReady && TerminalWebView.CoreWebView2 != null)
            {
                TerminalWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            else
            {
                _pendingScripts.Enqueue(script);
            }
        }

        private void FlushPendingScripts()
        {
            if (TerminalWebView.CoreWebView2 == null) return;

            while (_pendingScripts.Count > 0)
            {
                TerminalWebView.CoreWebView2.ExecuteScriptAsync(_pendingScripts.Dequeue());
            }
        }
    }
}
