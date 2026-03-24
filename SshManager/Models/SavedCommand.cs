using System;

namespace SshManager.Models
{
    /// <summary>
    /// Represents a saved command that can be run in a terminal.
    /// Commands can be global (all servers) or scoped to a specific session.
    /// </summary>
    public class SavedCommand
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public string Command { get; set; } = "";
        public string Notes { get; set; } = "";

        /// <summary>
        /// When null, the command is global (available for all servers).
        /// When set, the command is scoped to the session with this Id.
        /// </summary>
        public Guid? SessionId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public SavedCommand Clone()
        {
            return new SavedCommand
            {
                Id = this.Id,
                Name = this.Name,
                Command = this.Command,
                Notes = this.Notes,
                SessionId = this.SessionId,
                CreatedDate = this.CreatedDate
            };
        }
    }
}
