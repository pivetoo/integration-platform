using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class ProcessingQueueTests
    {
        private static readonly DateTimeOffset Now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        private static ProcessingQueue NewPending()
        {
            return new ProcessingQueue(connectorId: 2, pipelineId: 5, priority: 1, ProcessingStatus.Pending, payload: "{}");
        }

        [Test]
        public void Constructor_with_valid_data_sets_fields()
        {
            ProcessingQueue item = new(connectorId: 2, pipelineId: 5, priority: 3, ProcessingStatus.Pending, payload: "{}", scheduledAt: Now);

            item.ConnectorId.Should().Be(2);
            item.PipelineId.Should().Be(5);
            item.Priority.Should().Be(3);
            item.Status.Should().Be(ProcessingStatus.Pending);
            item.Payload.Should().Be("{}");
            item.ScheduledAt.Should().Be(Now);
            item.StartedAt.Should().BeNull();
            item.FinishedAt.Should().BeNull();
        }

        [TestCase(0L, 5L)]
        [TestCase(-1L, 5L)]
        [TestCase(2L, 0L)]
        [TestCase(2L, -1L)]
        public void Constructor_with_invalid_references_throws(long connectorId, long pipelineId)
        {
            Action act = () => new ProcessingQueue(connectorId, pipelineId, 1, ProcessingStatus.Pending);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void MarkAsProcessing_sets_processing_and_clears_finish_and_error()
        {
            ProcessingQueue item = NewPending();
            item.Fail("boom", Now);

            item.MarkAsProcessing(Now.AddMinutes(1));

            item.Status.Should().Be(ProcessingStatus.Processing);
            item.StartedAt.Should().Be(Now.AddMinutes(1));
            item.FinishedAt.Should().BeNull();
            item.LastError.Should().BeNull();
        }

        [Test]
        public void Complete_sets_completed_and_finish()
        {
            ProcessingQueue item = NewPending();

            item.Complete(Now);

            item.Status.Should().Be(ProcessingStatus.Completed);
            item.FinishedAt.Should().Be(Now);
        }

        [Test]
        public void Fail_sets_error_trimmed_message_and_finish()
        {
            ProcessingQueue item = NewPending();

            item.Fail("  network timeout  ", Now);

            item.Status.Should().Be(ProcessingStatus.Error);
            item.LastError.Should().Be("network timeout");
            item.FinishedAt.Should().Be(Now);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Fail_with_blank_error_throws(string? error)
        {
            ProcessingQueue item = NewPending();

            Action act = () => item.Fail(error!, Now);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Cancel_sets_cancelled_and_finish()
        {
            ProcessingQueue item = NewPending();

            item.Cancel(Now);

            item.Status.Should().Be(ProcessingStatus.Cancelled);
            item.FinishedAt.Should().Be(Now);
        }
    }
}
