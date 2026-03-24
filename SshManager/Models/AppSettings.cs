namespace SshManager.Models
{
    /// <summary>
    /// Application settings persisted to disk.
    /// </summary>
    public class AppSettings
    {
        // -- Window --
        public int WindowWidth { get; set; } = 1100;
        public int WindowHeight { get; set; } = 700;

        // -- Terminal --
        public int TerminalFontSize { get; set; } = 14;
        public int TerminalScrollback { get; set; } = 10000;

        // -- SSH defaults --
        public int DefaultPort { get; set; } = 22;
        public string DefaultUsername { get; set; } = "";
        public int ConnectionTimeoutSeconds { get; set; } = 15;

        // -- Behavior --
        public bool ConfirmOnCloseWithActiveSessions { get; set; } = true;
    }
}
