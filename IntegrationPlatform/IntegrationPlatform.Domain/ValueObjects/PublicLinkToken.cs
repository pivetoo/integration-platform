namespace IntegrationPlatform.Domain.ValueObjects
{
    // Formato do token de webhook publico: "{tenantId}~{aleatorio}".
    // O prefixo de tenant permite que o endpoint anonimo de webhook resolva o banco correto
    // (multi-tenant) sem JWT/secret. O separador '~' e URL-safe e nao aparece no aleatorio
    // (base64url) nem nos ids de tenant (GUID).
    public static class PublicLinkToken
    {
        private const char Separator = '~';

        public static string Compose(string? tenantId, string random)
        {
            return string.IsNullOrWhiteSpace(tenantId) ? random : $"{tenantId}{Separator}{random}";
        }

        public static string? ExtractTenantId(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            int index = token.IndexOf(Separator);
            return index > 0 ? token[..index] : null;
        }
    }
}
