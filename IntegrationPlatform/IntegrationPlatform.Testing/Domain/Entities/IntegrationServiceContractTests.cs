using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class IntegrationServiceContractTests
    {
        [Test]
        public void Constructor_sets_references()
        {
            IntegrationServiceContract binding = new(integrationId: 4, serviceContractId: 7);

            binding.IntegrationId.Should().Be(4);
            binding.ServiceContractId.Should().Be(7);
        }

        [TestCase(0L, 7L)]
        [TestCase(-1L, 7L)]
        [TestCase(4L, 0L)]
        [TestCase(4L, -1L)]
        public void Constructor_with_invalid_references_throws(long integrationId, long serviceContractId)
        {
            Action act = () => new IntegrationServiceContract(integrationId, serviceContractId);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Activate_and_Deactivate_toggle_flag()
        {
            IntegrationServiceContract binding = new(1, 1);

            binding.Deactivate();
            binding.IsActive.Should().BeFalse();

            binding.Activate();
            binding.IsActive.Should().BeTrue();
        }
    }
}
