using IntegrationPlatform.Infrastructure.Services.ExecutionEngine;

namespace IntegrationPlatform.Testing.Infrastructure.ExecutionEngine
{
    [TestFixture]
    public sealed class PipelinePayloadParserTests
    {
        [Test]
        public void TryParse_should_accept_null_or_whitespace_as_empty_payload()
        {
            PipelinePayloadParser.TryParse(null, out Dictionary<string, object> fromNull).Should().BeTrue();
            fromNull.Should().BeEmpty();

            PipelinePayloadParser.TryParse("   ", out Dictionary<string, object> fromBlank).Should().BeTrue();
            fromBlank.Should().BeEmpty();
        }

        [Test]
        public void TryParse_should_parse_a_json_object()
        {
            bool ok = PipelinePayloadParser.TryParse("{\"email\":\"a@b.com\",\"amount\":10}", out Dictionary<string, object> payload);

            ok.Should().BeTrue();
            payload.Should().ContainKey("email");
            payload.Should().ContainKey("amount");
        }

        [Test]
        public void TryParse_should_reject_malformed_json()
        {
            PipelinePayloadParser.TryParse("{ this is not json", out Dictionary<string, object> payload).Should().BeFalse();
            payload.Should().BeEmpty();
        }

        [Test]
        public void TryParse_should_reject_json_that_is_not_an_object()
        {
            PipelinePayloadParser.TryParse("[1,2,3]", out Dictionary<string, object> fromArray).Should().BeFalse();
            fromArray.Should().BeEmpty();

            PipelinePayloadParser.TryParse("5", out Dictionary<string, object> fromNumber).Should().BeFalse();
            fromNumber.Should().BeEmpty();
        }

        [Test]
        public void TryParse_should_treat_literal_json_null_as_empty_payload()
        {
            PipelinePayloadParser.TryParse("null", out Dictionary<string, object> payload).Should().BeTrue();
            payload.Should().BeEmpty();
        }
    }
}
