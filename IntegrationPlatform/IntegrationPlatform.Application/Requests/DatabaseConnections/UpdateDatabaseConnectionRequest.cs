using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.DatabaseConnections
{
    public sealed class UpdateDatabaseConnectionRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        public int Type { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Host { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Database { get; set; } = string.Empty;

        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Password { get; set; } = string.Empty;
    }
}
