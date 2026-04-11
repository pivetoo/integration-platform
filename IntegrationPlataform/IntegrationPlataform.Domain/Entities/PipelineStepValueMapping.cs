using Archon.Core.Entities;

namespace IntegrationPlataform.Domain.Entities
{
    public class PipelineStepValueMapping : Entity
    {
        public long PipelineStepId { get; private set; }

        public PipelineStep PipelineStep { get; private set; } = null!;

        public long? SourcePipelineStepId { get; private set; }

        public PipelineStep? SourcePipelineStep { get; private set; }

        public long? PipelineExampleId { get; private set; }

        public PipelineExample? PipelineExample { get; private set; }

        public string TargetField { get; private set; } = string.Empty;

        public string SourceType { get; private set; } = string.Empty;

        public string SourcePath { get; private set; } = string.Empty;

        public string ValueType { get; private set; } = string.Empty;

        public string? FixedValue { get; private set; }

        public int Order { get; private set; }

        private PipelineStepValueMapping()
        {
        }

        public PipelineStepValueMapping(long pipelineStepId, string targetField, string sourceType, string sourcePath, string valueType, int order, string? fixedValue = null, long? sourcePipelineStepId = null, long? pipelineExampleId = null)
        {
            if (pipelineStepId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipelineStepId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(targetField);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
            ArgumentException.ThrowIfNullOrWhiteSpace(valueType);

            PipelineStepId = pipelineStepId;
            SourcePipelineStepId = sourcePipelineStepId;
            PipelineExampleId = pipelineExampleId;
            TargetField = targetField.Trim();
            SourceType = sourceType.Trim();
            SourcePath = sourcePath.Trim();
            ValueType = valueType.Trim();
            FixedValue = fixedValue;
            Order = order;
        }

        public void Update(long pipelineStepId, string targetField, string sourceType, string sourcePath, string valueType, int order, string? fixedValue, long? sourcePipelineStepId, long? pipelineExampleId)
        {
            if (pipelineStepId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipelineStepId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(targetField);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
            ArgumentException.ThrowIfNullOrWhiteSpace(valueType);

            PipelineStepId = pipelineStepId;
            SourcePipelineStepId = sourcePipelineStepId;
            PipelineExampleId = pipelineExampleId;
            TargetField = targetField.Trim();
            SourceType = sourceType.Trim();
            SourcePath = sourcePath.Trim();
            ValueType = valueType.Trim();
            FixedValue = fixedValue;
            Order = order;
        }
    }
}
