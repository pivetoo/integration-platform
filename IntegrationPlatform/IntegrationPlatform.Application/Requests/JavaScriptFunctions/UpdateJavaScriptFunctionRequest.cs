using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.JavaScriptFunctions
{
    public sealed class UpdateJavaScriptFunctionRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [MinLength(1)]
        public string Code { get; set; } = string.Empty;
    }
}
