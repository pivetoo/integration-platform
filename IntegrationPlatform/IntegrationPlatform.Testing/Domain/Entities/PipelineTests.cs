using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class PipelineTests
    {
        [Test]
        public void Constructor_trims_and_sets_fields()
        {
            Pipeline pipeline = new(integrationId: 4, identifier: "  main-flow  ", name: "  Main  ", description: "  d  ");

            pipeline.IntegrationId.Should().Be(4);
            pipeline.Identifier.Should().Be("main-flow");
            pipeline.Name.Should().Be("Main");
            pipeline.Description.Should().Be("d");
            pipeline.IsActive.Should().BeTrue();
            pipeline.IsDefault.Should().BeFalse();
            pipeline.ServiceContractId.Should().BeNull();
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void Constructor_with_invalid_integrationId_throws(long integrationId)
        {
            Action act = () => new Pipeline(integrationId, "id", "name");

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase("", "name")]
        [TestCase("id", "  ")]
        public void Constructor_with_blank_identifier_or_name_throws(string identifier, string name)
        {
            Action act = () => new Pipeline(1, identifier, name);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void SetDefault_and_UnsetDefault_toggle_flag()
        {
            Pipeline pipeline = new(1, "id", "name");

            pipeline.SetDefault();
            pipeline.IsDefault.Should().BeTrue();

            pipeline.UnsetDefault();
            pipeline.IsDefault.Should().BeFalse();
        }

        [Test]
        public void BindServiceContract_sets_id_and_Unbind_clears_it()
        {
            Pipeline pipeline = new(1, "id", "name");

            pipeline.BindServiceContract(12);
            pipeline.ServiceContractId.Should().Be(12);

            pipeline.UnbindServiceContract();
            pipeline.ServiceContractId.Should().BeNull();
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void BindServiceContract_with_invalid_id_throws(long serviceContractId)
        {
            Pipeline pipeline = new(1, "id", "name");

            Action act = () => pipeline.BindServiceContract(serviceContractId);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Update_changes_fields_and_active_flag()
        {
            Pipeline pipeline = new(1, "old", "Old");

            pipeline.Update(2, "  new  ", "  New  ", "desc", isActive: false);

            pipeline.IntegrationId.Should().Be(2);
            pipeline.Identifier.Should().Be("new");
            pipeline.Name.Should().Be("New");
            pipeline.IsActive.Should().BeFalse();
        }

        [Test]
        public void MaxAttempts_defaults_to_one()
        {
            new Pipeline(1, "p", "P").MaxAttempts.Should().Be(1);
        }

        [TestCase(1)]
        [TestCase(10)]
        public void SetMaxAttempts_accepts_range(int value)
        {
            Pipeline pipeline = new(1, "p", "P");

            pipeline.SetMaxAttempts(value);

            pipeline.MaxAttempts.Should().Be(value);
        }

        [TestCase(0)]
        [TestCase(11)]
        [TestCase(-1)]
        public void SetMaxAttempts_outside_range_throws(int value)
        {
            Pipeline pipeline = new(1, "p", "P");

            Action act = () => pipeline.SetMaxAttempts(value);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
