using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Api.Contracts.Execution
{
    public sealed class NextDebugStepRequest
    {
        [Required]
        public string DebugSessionId { get; set; } = string.Empty;
    }
}
