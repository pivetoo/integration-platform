using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class PipelineStepTests
    {
        [Test]
        public void Constructor_trims_name_and_sets_fields()
        {
            PipelineStep step = new(pipelineId: 5, order: 2, name: "  Call API  ", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCallId: 8, ignoreOnResponse: true);

            step.PipelineId.Should().Be(5);
            step.Order.Should().Be(2);
            step.Name.Should().Be("Call API");
            step.Type.Should().Be(PipelineStepType.HttpRequest);
            step.ErrorAction.Should().Be(ErrorAction.Stop);
            step.ApiCallId.Should().Be(8);
            step.IgnoreOnResponse.Should().BeTrue();
            step.IsActive.Should().BeTrue();
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void Constructor_with_invalid_pipelineId_throws(long pipelineId)
        {
            Action act = () => new PipelineStep(pipelineId, 1, "name", PipelineStepType.HttpRequest, ErrorAction.Stop);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_with_blank_name_throws(string name)
        {
            Action act = () => new PipelineStep(1, 1, name, PipelineStepType.HttpRequest, ErrorAction.Stop);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_changes_fields_and_active_flag()
        {
            PipelineStep step = new(1, 1, "old", PipelineStepType.HttpRequest, ErrorAction.Stop);

            step.Update(order: 3, name: "  new  ", PipelineStepType.JavaScriptFunction, ErrorAction.Continue, apiCallId: null, javaScriptFunctionId: 9, databaseScriptId: null, isActive: false, ignoreOnResponse: true);

            step.Order.Should().Be(3);
            step.Name.Should().Be("new");
            step.Type.Should().Be(PipelineStepType.JavaScriptFunction);
            step.ErrorAction.Should().Be(ErrorAction.Continue);
            step.JavaScriptFunctionId.Should().Be(9);
            step.ApiCallId.Should().BeNull();
            step.IsActive.Should().BeFalse();
            step.IgnoreOnResponse.Should().BeTrue();
        }
    }
}
