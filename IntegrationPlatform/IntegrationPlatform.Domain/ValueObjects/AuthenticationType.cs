namespace IntegrationPlatform.Domain.ValueObjects
{
    public enum AuthenticationType
    {
        None = 1,
        Bearer = 2,
        Basic = 3,
        ApiKey = 4,
        Custom = 5
    }
}
