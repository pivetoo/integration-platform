using Archon.Core.Entities;

namespace IntegrationPlatform.Domain.Entities
{
    public class Pipeline : Entity
    {
        private readonly List<PipelineStep> steps = [];
        private readonly List<Execution> executions = [];
        private readonly List<ProcessingQueue> processingQueues = [];
        private readonly List<PipelineRoutine> routines = [];

        public long IntegrationId { get; private set; }

        public Integration Integration { get; private set; } = null!;

        public string Identifier { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public bool IsActive { get; private set; } = true;

        public bool IsDefault { get; private set; }

        public IReadOnlyCollection<PipelineStep> Steps => steps.AsReadOnly();

        public IReadOnlyCollection<Execution> Executions => executions.AsReadOnly();

        public IReadOnlyCollection<ProcessingQueue> ProcessingQueues => processingQueues.AsReadOnly();

        public IReadOnlyCollection<PipelineRoutine> Routines => routines.AsReadOnly();

        private Pipeline()
        {
        }

        public Pipeline(long integrationId, string identifier, string name, string? description = null)
        {
            if (integrationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(integrationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            IntegrationId = integrationId;
            Identifier = identifier.Trim();
            Name = name.Trim();
            Description = description?.Trim();
        }

        public void SetDefault()
        {
            IsDefault = true;
        }

        public void UnsetDefault()
        {
            IsDefault = false;
        }

        public void Update(long integrationId, string identifier, string name, string? description, bool isActive)
        {
            if (integrationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(integrationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            IntegrationId = integrationId;
            Identifier = identifier.Trim();
            Name = name.Trim();
            Description = description?.Trim();
            IsActive = isActive;
        }
    }
}
