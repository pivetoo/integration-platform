namespace IntegrationPlatform.Infrastructure.BackgroundJobs
{
    public sealed class BackgroundJobOptions
    {
        public bool SchedulerEnabled { get; set; } = true;

        public bool QueueWorkerEnabled { get; set; } = true;

        public TimeSpan SchedulerPollingInterval { get; set; } = TimeSpan.FromSeconds(30);

        public TimeSpan QueueWorkerPollingInterval { get; set; } = TimeSpan.FromSeconds(5);

        public int MaxItemsPerCycle { get; set; } = 10;
    }
}
