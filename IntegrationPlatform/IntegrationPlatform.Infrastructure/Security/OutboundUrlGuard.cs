using System.Net;
using System.Net.Sockets;

namespace IntegrationPlatform.Infrastructure.Security
{
    // Protecao contra SSRF para requests de saida (passo HTTP, callback). Bloqueia loopback,
    // redes privadas (RFC1918), link-local (inclui 169.254.169.254 de metadados de cloud), CGNAT,
    // e ULA IPv6. Para hostnames, resolve o DNS e exige que TODOS os IPs resolvidos sejam publicos.
    public static class OutboundUrlGuard
    {
        public static bool IsBlockedAddress(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
            {
                return true;
            }

            // Enderecos "unspecified" (0.0.0.0 e ::) podem rotear para servicos locais.
            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                return true;
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
            {
                return IsBlockedAddress(address.MapToIPv4());
            }

            byte[] bytes = address.GetAddressBytes();

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return bytes[0] switch
                {
                    0 => true,                                   // 0.0.0.0/8
                    10 => true,                                  // 10.0.0.0/8
                    127 => true,                                 // 127.0.0.0/8 (tambem coberto por IsLoopback)
                    169 when bytes[1] == 254 => true,            // 169.254.0.0/16 link-local + metadata
                    172 when bytes[1] >= 16 && bytes[1] <= 31 => true, // 172.16.0.0/12
                    192 when bytes[1] == 168 => true,            // 192.168.0.0/16
                    100 when bytes[1] >= 64 && bytes[1] <= 127 => true, // 100.64.0.0/10 (CGNAT)
                    _ => false
                };
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
                {
                    return true;
                }

                // fc00::/7 unique local address
                return (bytes[0] & 0xFE) == 0xFC;
            }

            return false;
        }

        public static async Task<bool> IsAllowedAsync(string? url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            string host = uri.Host;

            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (IPAddress.TryParse(host, out IPAddress? literal))
            {
                return !IsBlockedAddress(literal);
            }

            try
            {
                IPAddress[] resolved = await Dns.GetHostAddressesAsync(host, cancellationToken);
                return resolved.Length > 0 && resolved.All(address => !IsBlockedAddress(address));
            }
            catch
            {
                return false;
            }
        }
    }
}
