using System;
using Renci.SshNet;
using SshManager.Helpers;
using SshManager.Models;

namespace SshManager.Services
{
    /// <summary>
    /// Connects to SSH servers using SSH.NET.
    /// Supports password and private key authentication.
    /// </summary>
    public class SshConnectionService : ISshConnectionService
    {
        public int ConnectionTimeoutSeconds { get; set; } = 15;

        public SshClient Connect(SshSession session)
        {
            return Connect(session, null);
        }

        public SshClient Connect(SshSession session, string plaintextPassword)
        {
            ConnectionInfo connectionInfo;

            if (session.AuthMethod == AuthMethod.PrivateKey)
            {
                var keyFile = new PrivateKeyFile(session.PrivateKeyPath);
                connectionInfo = new ConnectionInfo(
                    session.Host, session.Port, session.Username,
                    new PrivateKeyAuthenticationMethod(session.Username, keyFile));
            }
            else if (session.AuthMethod == AuthMethod.PromptPassword && plaintextPassword != null)
            {
                connectionInfo = new ConnectionInfo(
                    session.Host, session.Port, session.Username,
                    new PasswordAuthenticationMethod(session.Username, plaintextPassword));
            }
            else
            {
                var password = PasswordHelper.Decrypt(session.EncryptedPassword);
                connectionInfo = new ConnectionInfo(
                    session.Host, session.Port, session.Username,
                    new PasswordAuthenticationMethod(session.Username, password));
            }

            connectionInfo.Timeout = TimeSpan.FromSeconds(ConnectionTimeoutSeconds);

            var client = new SshClient(connectionInfo);
            client.Connect();
            return client;
        }

        public ShellStream CreateShellStream(SshClient client)
        {
            return client.CreateShellStream("xterm-256color", 120, 40, 800, 600, 4096);
        }
    }
}
