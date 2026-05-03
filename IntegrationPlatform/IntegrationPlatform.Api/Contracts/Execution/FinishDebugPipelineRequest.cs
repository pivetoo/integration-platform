using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Api.Contracts.Execution
{
    public sealed class FinishDebugPipelineRequest
    {
        [Required]
        public string DebugSessionId { get; set; } = string.Empty;
    }
}
