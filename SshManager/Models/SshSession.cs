using System;

namespace SshManager.Models
{
    /// <summary>
    /// How the SSH session authenticates.
    /// </summary>
    public enum AuthMethod
    {
        Password,
        PrivateKey,
        PromptPassword
    }

    /// <summary>
    /// Represents a saved/bookmarked SSH session.
    /// </summary>
    public class SshSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public string Host { get; set; } = "";
        public int Port { get; set; } = 22;
        public string Username { get; set; } = "";
        public AuthMethod AuthMethod { get; set; } = AuthMethod.Password;
        public string EncryptedPassword { get; set; } = "";
        public string PrivateKeyPath { get; set; } = "";
        public string Group { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastConnectedDate { get; set; }

        public SshSession Clone()
        {
            return new SshSession
            {
                Id = this.Id,
                Name = this.Name,
                Host = this.Host,
                Port = this.Port,
                Username = this.Username,
                AuthMethod = this.AuthMethod,
                EncryptedPassword = this.EncryptedPassword,
                PrivateKeyPath = this.PrivateKeyPath,
                Group = this.Group,
                Notes = this.Notes,
                CreatedDate = this.CreatedDate,
                LastConnectedDate = this.LastConnectedDate
            };
        }
    }
}
