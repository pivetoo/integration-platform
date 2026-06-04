using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class ServiceContractTests
    {
        [Test]
        public void Constructor_normalizes_identifier_and_trims_name()
        {
            ServiceContract contract = new("  Email.Send  ", "  Envio de email  ", integrationCategoryId: 4);

            contract.Identifier.Should().Be("email.send");
            contract.Name.Should().Be("Envio de email");
            contract.IsActive.Should().BeTrue();
            contract.IncludeOutputInCallback.Should().BeFalse();
            contract.IsSystem.Should().BeFalse();
        }

        [TestCase("", "name")]
        [TestCase("   ", "name")]
        [TestCase("id", "")]
        [TestCase("id", "   ")]
        public void Constructor_with_blank_identifier_or_name_throws(string identifier, string name)
        {
            Action act = () => new ServiceContract(identifier, name, 1);

            act.Should().Throw<ArgumentException>();
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void Constructor_with_invalid_category_throws(long categoryId)
        {
            Action act = () => new ServiceContract("id", "name", categoryId);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void SetCallbackOutputInclusion_toggles_flag()
        {
            ServiceContract contract = new("svc", "Service", 1);

            contract.SetCallbackOutputInclusion(true);
            contract.IncludeOutputInCallback.Should().BeTrue();

            contract.SetCallbackOutputInclusion(false);
            contract.IncludeOutputInCallback.Should().BeFalse();
        }

        [Test]
        public void Update_on_system_contract_changing_identifier_throws()
        {
            ServiceContract contract = new("system.svc", "System", 1, isSystem: true);

            Action act = () => contract.Update("system.changed", "System", 1, null, null, null, false, null, true);

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Update_on_system_contract_keeping_identifier_succeeds()
        {
            ServiceContract contract = new("system.svc", "System", 1, isSystem: true);

            contract.Update("System.Svc", "Renamed", 2, "desc", null, null, true, null, false);

            contract.Name.Should().Be("Renamed");
            contract.IntegrationCategoryId.Should().Be(2);
            contract.HasCallback.Should().BeTrue();
            contract.IsActive.Should().BeFalse();
        }

        [Test]
        public void Update_on_non_system_contract_can_change_identifier()
        {
            ServiceContract contract = new("svc.old", "Service", 1);

            contract.Update("svc.new", "Service", 1, null, null, null, false, null, true);

            contract.Identifier.Should().Be("svc.new");
        }
    }
}
