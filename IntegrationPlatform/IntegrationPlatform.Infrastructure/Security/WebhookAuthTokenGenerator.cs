using System.Security.Cryptography;

namespace IntegrationPlatform.Infrastructure.Security
{
    // Segredo enviado ao provedor como authToken do webhook (ex.: Asaas) e validado de volta no header da
    // notificacao. A Asaas recusa token com mais de 4 caracteres iguais consecutivos ("O token nao pode
    // conter mais de 4 caracteres iguais consecutivos"); 64 hex aleatorios caem nessa regra de vez em
    // quando e, como o token e gerado uma unica vez por conector, o registro ficava travado ate alguem
    // apagar o atributo na mao. Por isso a geracao rejeita e sorteia de novo.
    public static class WebhookAuthTokenGenerator
    {
        public const int MaxIdenticalRun = 4;

        public static string Generate()
        {
            while (true)
            {
                string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                if (!HasIdenticalRunLongerThan(token, MaxIdenticalRun))
                {
                    return token;
                }
            }
        }

        public static bool HasIdenticalRunLongerThan(string value, int maxRun)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            int run = 1;
            for (int index = 1; index < value.Length; index++)
            {
                run = value[index] == value[index - 1] ? run + 1 : 1;
                if (run > maxRun)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
