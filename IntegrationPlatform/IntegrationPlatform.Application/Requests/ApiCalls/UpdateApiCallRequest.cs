using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.ApiCalls
{
    public sealed class UpdateApiCallRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int Method { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Url { get; set; } = string.Empty;

        public string? HeadersTemplate { get; set; }

        public string? BodyTemplate { get; set; }
    }
}
