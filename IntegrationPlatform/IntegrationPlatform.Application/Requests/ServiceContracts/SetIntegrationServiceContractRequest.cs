using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.ServiceContracts
{
    public sealed class SetIntegrationServiceContractRequest
    {
        [Required]
        public long IntegrationId { get; set; }

        [Required]
        public long ServiceContractId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
