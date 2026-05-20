using Archon.Core.Entities;

namespace IntegrationPlatform.Domain.Entities
{
    public class IntegrationServiceContract : Entity
    {
        public long IntegrationId { get; private set; }

        public Integration Integration { get; private set; } = null!;

        public long ServiceContractId { get; private set; }

        public ServiceContract ServiceContract { get; private set; } = null!;

        public bool IsActive { get; private set; } = true;

        private IntegrationServiceContract()
        {
        }

        public IntegrationServiceContract(long integrationId, long serviceContractId)
        {
            if (integrationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(integrationId));
            }

            if (serviceContractId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(serviceContractId));
            }

            IntegrationId = integrationId;
            ServiceContractId = serviceContractId;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
