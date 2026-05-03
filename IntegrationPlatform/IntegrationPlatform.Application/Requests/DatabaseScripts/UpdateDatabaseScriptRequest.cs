using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.DatabaseScripts
{
    public sealed class UpdateDatabaseScriptRequest
    {
        [Required]
        public long Id { get; set; }

        [Range(1, long.MaxValue)]
        public long DatabaseConnectionId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [MinLength(1)]
        public string Script { get; set; } = string.Empty;
    }
}
