using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class ExecutionLogTests
    {
        [Test]
        public void Constructor_trims_message_and_sets_fields()
        {
            ExecutionLog log = new(executionId: 10, LogLevelType.Error, message: "  boom  ", pipelineStepId: 4, context: "ctx", request: "req", response: "res", httpStatusCode: 500, duration: 120);

            log.ExecutionId.Should().Be(10);
            log.Level.Should().Be(LogLevelType.Error);
            log.Message.Should().Be("boom");
            log.PipelineStepId.Should().Be(4);
            log.Context.Should().Be("ctx");
            log.Request.Should().Be("req");
            log.Response.Should().Be("res");
            log.HttpStatusCode.Should().Be(500);
            log.Duration.Should().Be(120);
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void Constructor_with_invalid_executionId_throws(long executionId)
        {
            Action act = () => new ExecutionLog(executionId, LogLevelType.Info, "msg");

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_with_blank_message_throws(string message)
        {
            Action act = () => new ExecutionLog(1, LogLevelType.Info, message);

            act.Should().Throw<ArgumentException>();
        }
    }
}
