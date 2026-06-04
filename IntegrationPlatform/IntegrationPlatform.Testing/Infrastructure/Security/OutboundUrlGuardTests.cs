using System.Net;
using IntegrationPlatform.Infrastructure.Security;

namespace IntegrationPlatform.Testing.Infrastructure.Security
{
    [TestFixture]
    public sealed class OutboundUrlGuardTests
    {
        [TestCase("127.0.0.1", true)]
        [TestCase("10.0.0.5", true)]
        [TestCase("172.16.0.1", true)]
        [TestCase("172.31.255.255", true)]
        [TestCase("172.15.0.1", false)]
        [TestCase("172.32.0.1", false)]
        [TestCase("192.168.1.1", true)]
        [TestCase("169.254.169.254", true)]
        [TestCase("0.0.0.0", true)]
        [TestCase("::", true)]
        [TestCase("100.64.0.1", true)]
        [TestCase("8.8.8.8", false)]
        [TestCase("1.1.1.1", false)]
        [TestCase("93.184.216.34", false)]
        [TestCase("::1", true)]
        [TestCase("fc00::1", true)]
        [TestCase("fd12:3456::1", true)]
        [TestCase("2606:4700:4700::1111", false)]
        public void IsBlockedAddress_classifies_private_and_public(string ip, bool blocked)
        {
            OutboundUrlGuard.IsBlockedAddress(IPAddress.Parse(ip)).Should().Be(blocked);
        }
    }
}
