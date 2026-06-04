using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class PipelineRoutineTests
    {
        private static readonly DateTimeOffset Now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        [Test]
        public void Constructor_sets_fields()
        {
            PipelineRoutine routine = new(connectorId: 2, pipelineId: 5, intervalInMinutes: 15, isActive: true, defaultPayload: "{}");

            routine.ConnectorId.Should().Be(2);
            routine.PipelineId.Should().Be(5);
            routine.IntervalInMinutes.Should().Be(15);
            routine.IsActive.Should().BeTrue();
            routine.DefaultPayload.Should().Be("{}");
        }

        [TestCase(0L, 5L, 15)]
        [TestCase(-1L, 5L, 15)]
        [TestCase(2L, 0L, 15)]
        [TestCase(2L, 5L, 0)]
        [TestCase(2L, 5L, -1)]
        public void Constructor_with_invalid_arguments_throws(long connectorId, long pipelineId, int interval)
        {
            Action act = () => new PipelineRoutine(connectorId, pipelineId, interval);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Update_changes_interval_active_payload_and_next_execution()
        {
            PipelineRoutine routine = new(1, 1, 10);

            routine.Update(30, isActive: false, defaultPayload: "{\"x\":1}", nextExecutionAt: Now);

            routine.IntervalInMinutes.Should().Be(30);
            routine.IsActive.Should().BeFalse();
            routine.DefaultPayload.Should().Be("{\"x\":1}");
            routine.NextExecutionAt.Should().Be(Now);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Update_with_invalid_interval_throws(int interval)
        {
            PipelineRoutine routine = new(1, 1, 10);

            Action act = () => routine.Update(interval, true, null, null);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void MarkExecution_records_last_and_next()
        {
            PipelineRoutine routine = new(1, 1, 10);

            routine.MarkExecution(Now, Now.AddMinutes(10));

            routine.LastExecutionAt.Should().Be(Now);
            routine.NextExecutionAt.Should().Be(Now.AddMinutes(10));
        }
    }
}
