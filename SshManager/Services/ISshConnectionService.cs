using Renci.SshNet;
using SshManager.Models;

namespace SshManager.Services
{
    /// <summary>
    /// Creates SSH connections and shell streams.
    /// </summary>
    public interface ISshConnectionService
    {
        SshClient Connect(SshSession session);
        SshClient Connect(SshSession session, string plaintextPassword);
        ShellStream CreateShellStream(SshClient client);
    }
}
