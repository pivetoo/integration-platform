using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class ExecutionTests
    {
        private static readonly DateTimeOffset Start = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        [Test]
        public void Constructor_with_valid_data_sets_fields()
        {
            Execution execution = new(ExecutionType.Pipeline, connectorId: 7, ExecutionStatus.Running, Start, pipelineId: 3, processingQueueId: 9, inputData: "{}");

            execution.Type.Should().Be(ExecutionType.Pipeline);
            execution.ConnectorId.Should().Be(7);
            execution.PipelineId.Should().Be(3);
            execution.ProcessingQueueId.Should().Be(9);
            execution.Status.Should().Be(ExecutionStatus.Running);
            execution.InputData.Should().Be("{}");
            execution.StartedAt.Should().Be(Start);
            execution.FinishedAt.Should().BeNull();
            execution.Duration.Should().BeNull();
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void Constructor_with_invalid_connectorId_throws(long connectorId)
        {
            Action act = () => new Execution(ExecutionType.Pipeline, connectorId, ExecutionStatus.Running, Start);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void MarkAsRunning_resets_finish_and_duration()
        {
            Execution execution = new(ExecutionType.Pipeline, 1, ExecutionStatus.Running, Start);
            execution.Complete(ExecutionStatus.Success, Start.AddSeconds(2), "out", null);

            execution.MarkAsRunning();

            execution.Status.Should().Be(ExecutionStatus.Running);
            execution.FinishedAt.Should().BeNull();
            execution.Duration.Should().BeNull();
        }

        [Test]
        public void Complete_sets_status_output_errors_and_duration()
        {
            Execution execution = new(ExecutionType.Pipeline, 1, ExecutionStatus.Running, Start);
            DateTimeOffset finishedAt = Start.AddMilliseconds(1500);

            execution.Complete(ExecutionStatus.Partial, finishedAt, "output", "warn");

            execution.Status.Should().Be(ExecutionStatus.Partial);
            execution.FinishedAt.Should().Be(finishedAt);
            execution.OutputData.Should().Be("output");
            execution.Errors.Should().Be("warn");
            execution.Duration.Should().Be(1500);
        }

        [Test]
        public void UpdateInput_replaces_input_data()
        {
            Execution execution = new(ExecutionType.Webhook, 1, ExecutionStatus.Running, Start, inputData: "old");

            execution.UpdateInput("{\"a\":1}");

            execution.InputData.Should().Be("{\"a\":1}");
        }
    }
}
