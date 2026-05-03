namespace IntegrationPlatform.Domain.ValueObjects
{
    public enum ProcessingStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Error = 4,
        Cancelled = 5
    }
}
