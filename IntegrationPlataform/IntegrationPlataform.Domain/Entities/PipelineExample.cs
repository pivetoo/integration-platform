using Archon.Core.Entities;

namespace IntegrationPlataform.Domain.Entities
{
    public class PipelineExample : Entity
    {
        private readonly List<PipelineStepExample> stepExamples = [];

        public long PipelineId { get; private set; }

        public Pipeline Pipeline { get; private set; } = null!;

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public string? InputPayloadExample { get; private set; }

        public string? ExpectedOutputExample { get; private set; }

        public bool IsDefault { get; private set; }

        public IReadOnlyCollection<PipelineStepExample> StepExamples => stepExamples.AsReadOnly();

        private PipelineExample()
        {
        }

        public PipelineExample(long pipelineId, string name, string? inputPayloadExample = null, string? expectedOutputExample = null, string? description = null, bool isDefault = false)
        {
            if (pipelineId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipelineId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            PipelineId = pipelineId;
            Name = name.Trim();
            Description = description?.Trim();
            InputPayloadExample = inputPayloadExample;
            ExpectedOutputExample = expectedOutputExample;
            IsDefault = isDefault;
        }

        public void Update(long pipelineId, string name, string? inputPayloadExample, string? expectedOutputExample, string? description, bool isDefault)
        {
            if (pipelineId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipelineId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            PipelineId = pipelineId;
            Name = name.Trim();
            Description = description?.Trim();
            InputPayloadExample = inputPayloadExample;
            ExpectedOutputExample = expectedOutputExample;
            IsDefault = isDefault;
        }
    }
}
