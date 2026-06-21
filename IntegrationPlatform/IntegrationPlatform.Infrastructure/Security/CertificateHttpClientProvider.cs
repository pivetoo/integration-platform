using System.Collections.Concurrent;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace IntegrationPlatform.Infrastructure.Security
{
    public interface ICertificateHttpClientProvider
    {
        HttpClient GetClient(string pfxBase64, string? password);
    }

    // Fornece (e cacheia) um HttpClient com certificado cliente (mTLS / A1) carregado de um .pfx em base64.
    // O cache e por conteudo do certificado + senha, entao trocar o certificado gera um client novo automaticamente.
    public sealed class CertificateHttpClientProvider : ICertificateHttpClientProvider
    {
        private readonly ConcurrentDictionary<string, HttpClient> clients = new();

        public HttpClient GetClient(string pfxBase64, string? password)
        {
            string key = CacheKey(pfxBase64, password);
            return clients.GetOrAdd(key, _ => Build(pfxBase64, password));
        }

        private static string CacheKey(string pfxBase64, string? password)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(pfxBase64 + " " + (password ?? string.Empty)));
            return Convert.ToHexString(hash);
        }

        private static HttpClient Build(string pfxBase64, string? password)
        {
            byte[] pfx = Convert.FromBase64String(pfxBase64.Trim());
            X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12(pfx, password);

            SocketsHttpHandler handler = new()
            {
                AllowAutoRedirect = false,
                SslOptions = new SslClientAuthenticationOptions
                {
                    ClientCertificates = new X509CertificateCollection { certificate }
                }
            };

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
        }
    }
}
