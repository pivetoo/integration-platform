namespace IntegrationPlatform.Infrastructure.BackgroundJobs
{
    public sealed class BackgroundJobOptions
    {
        public bool SchedulerEnabled { get; set; } = true;

        public bool QueueWorkerEnabled { get; set; } = true;

        public TimeSpan SchedulerPollingInterval { get; set; } = TimeSpan.FromSeconds(30);

        public TimeSpan QueueWorkerPollingInterval { get; set; } = TimeSpan.FromSeconds(5);

        public int MaxItemsPerCycle { get; set; } = 10;

        public TimeSpan StuckItemTimeout { get; set; } = TimeSpan.FromMinutes(10);

        public bool CallbackDeliveryEnabled { get; set; } = true;

        public TimeSpan CallbackDeliveryPollingInterval { get; set; } = TimeSpan.FromSeconds(15);

        public int CallbackMaxAttempts { get; set; } = 6;

        public double CallbackBaseBackoffSeconds { get; set; } = 30;
    }
}
