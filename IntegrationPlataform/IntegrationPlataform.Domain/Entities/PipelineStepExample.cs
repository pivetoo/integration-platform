using Archon.Core.Entities;

namespace IntegrationPlataform.Domain.Entities
{
    public class PipelineStepExample : Entity
    {
        public long PipelineStepId { get; private set; }

        public PipelineStep PipelineStep { get; private set; } = null!;

        public long? PipelineExampleId { get; private set; }

        public PipelineExample? PipelineExample { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string? RequestExample { get; private set; }

        public string? ResponseExample { get; private set; }

        public bool IsDefault { get; private set; }

        private PipelineStepExample()
        {
        }

        public PipelineStepExample(long pipelineStepId, string name, string? requestExample = null, string? responseExample = null, long? pipelineExampleId = null, bool isDefault = false)
        {
            if (pipelineStepId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipelineStepId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            PipelineStepId = pipelineStepId;
            PipelineExampleId = pipelineExampleId;
            Name = name.Trim();
            RequestExample = requestExample;
            ResponseExample = responseExample;
            IsDefault = isDefault;
        }

        public void Update(long pipelineStepId, string name, string? requestExample, string? responseExample, long? pipelineExampleId, bool isDefault)
        {
            if (pipelineStepId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipelineStepId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            PipelineStepId = pipelineStepId;
            PipelineExampleId = pipelineExampleId;
            Name = name.Trim();
            RequestExample = requestExample;
            ResponseExample = responseExample;
            IsDefault = isDefault;
        }
    }
}
