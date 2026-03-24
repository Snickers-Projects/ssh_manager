using System.IO;
using System.Reflection;

namespace SshManager.Helpers
{
    /// <summary>
    /// Builds a self-contained HTML page with xterm.js inlined.
    /// All resources are embedded in the assembly — no internet needed.
    /// </summary>
    public static class TerminalHtmlBuilder
    {
        private const string HtmlTemplate = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"" />
<style>
html, body {
    margin: 0;
    padding: 0;
    width: 100%;
    height: 100%;
    overflow: hidden;
    background: #1e1e1e;
}
#terminal {
    width: 100%;
    height: 100%;
}
</style>
<style>/* XTERM_CSS */</style>
<script>/* XTERM_JS */</script>
<script>/* FIT_JS */</script>
</head>
<body>
<div id=""terminal""></div>
<script>
(function() {
    var term = new Terminal({
        cursorBlink: true,
        fontSize: /* FONT_SIZE */,
        fontFamily: 'Consolas, ""Courier New"", monospace',
        theme: {
            background: '#1e1e1e',
            foreground: '#cccccc',
            cursor: '#ffffff'
        },
        scrollback: /* SCROLLBACK */,
        convertEol: false
    });

    var fitAddon = new FitAddon.FitAddon();
    term.loadAddon(fitAddon);
    term.open(document.getElementById('terminal'));
    fitAddon.fit();

    window.addEventListener('resize', function() { fitAddon.fit(); });

    // Send user keystrokes to C# host
    term.onData(function(data) {
        window.chrome.webview.postMessage(JSON.stringify({ type: 'input', data: data }));
    });

    // Send terminal resize events to C# host
    term.onResize(function(size) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'resize', cols: size.cols, rows: size.rows
        }));
    });

    // Called by C# to write SSH data (base64-encoded bytes)
    window.writeData = function(base64) {
        var binary = atob(base64);
        var bytes = new Uint8Array(binary.length);
        for (var i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        term.write(bytes);
    };

    // Called by C# to write a plain status message
    window.writeStatus = function(msg) {
        term.write(msg);
    };

    // Signal to C# that the terminal is ready
    window.chrome.webview.postMessage(JSON.stringify({ type: 'ready' }));
})();
</script>
</body>
</html>";

        private static int _cachedFontSize;
        private static int _cachedScrollback;

        public static string Build(int fontSize = 14, int scrollback = 10000)
        {
            var xtermCss = LoadResource("SshManager.Resources.Terminal.xterm.css");
            var xtermJs = LoadResource("SshManager.Resources.Terminal.xterm.js");
            var fitJs = LoadResource("SshManager.Resources.Terminal.xterm-addon-fit.js");

            _cachedFontSize = fontSize;
            _cachedScrollback = scrollback;

            return HtmlTemplate
                .Replace("/* XTERM_CSS */", xtermCss)
                .Replace("/* XTERM_JS */", xtermJs)
                .Replace("/* FIT_JS */", fitJs)
                .Replace("/* FONT_SIZE */", fontSize.ToString())
                .Replace("/* SCROLLBACK */", scrollback.ToString());
        }

        private static string LoadResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(name))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
