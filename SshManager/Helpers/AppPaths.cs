using System.IO;
using System.Reflection;

namespace SshManager.Helpers
{
    /// <summary>
    /// Provides the portable data folder path (next to the executable).
    /// All app data (settings, sessions, commands, WebView2 cache) lives here.
    /// </summary>
    public static class AppPaths
    {
        private static string _dataFolder;

        /// <summary>
        /// Returns the "Data" folder next to the executable.
        /// Creates it if it doesn't exist.
        /// </summary>
        public static string DataFolder
        {
            get
            {
                if (_dataFolder == null)
                {
                    var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    _dataFolder = Path.Combine(exeDir, "Data");
                    Directory.CreateDirectory(_dataFolder);
                }
                return _dataFolder;
            }
        }

        /// <summary>
        /// Returns the WebView2 user data folder inside the data folder.
        /// </summary>
        public static string WebView2UserDataFolder
        {
            get
            {
                var folder = Path.Combine(DataFolder, "WebView2");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }
    }
}
