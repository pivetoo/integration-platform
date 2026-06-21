using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using IntegrationPlatform.Infrastructure.Security;

namespace IntegrationPlatform.Testing.Infrastructure.Security
{
    [TestFixture]
    public sealed class CertificateHttpClientProviderTests
    {
        private static string GeneratePfxBase64(string password)
        {
            using RSA rsa = RSA.Create(2048);
            CertificateRequest request = new("CN=mtls-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            return Convert.ToBase64String(certificate.Export(X509ContentType.Pfx, password));
        }

        [Test]
        public void GetClient_should_return_client_for_valid_pfx()
        {
            CertificateHttpClientProvider provider = new();
            string pfx = GeneratePfxBase64("testpass");

            HttpClient client = provider.GetClient(pfx, "testpass");

            client.Should().NotBeNull();
        }

        [Test]
        public void GetClient_should_cache_by_certificate_and_password()
        {
            CertificateHttpClientProvider provider = new();
            string pfx = GeneratePfxBase64("testpass");

            HttpClient first = provider.GetClient(pfx, "testpass");
            HttpClient second = provider.GetClient(pfx, "testpass");

            second.Should().BeSameAs(first);
        }

        [Test]
        public void GetClient_should_return_distinct_client_for_different_certificate()
        {
            CertificateHttpClientProvider provider = new();
            string firstPfx = GeneratePfxBase64("testpass");
            string secondPfx = GeneratePfxBase64("testpass");

            HttpClient firstClient = provider.GetClient(firstPfx, "testpass");
            HttpClient secondClient = provider.GetClient(secondPfx, "testpass");

            secondClient.Should().NotBeSameAs(firstClient);
        }
    }
}
