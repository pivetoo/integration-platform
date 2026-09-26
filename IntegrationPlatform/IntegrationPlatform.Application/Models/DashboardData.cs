namespace IntegrationPlatform.Application.Models
{
    public class DashboardKpis
    {
        public long ActiveIntegrations { get; set; }

        public long ActiveConnectors { get; set; }

        public long ActivePipelines { get; set; }

        public long ExecutionsToday { get; set; }

        public double SuccessRate { get; set; }

        public long ErrorsToday { get; set; }

        public long QueuePending { get; set; }

        public long? AverageDurationTodayMs { get; set; }
    }

    public class QueueStatusSummary
    {
        public int Status { get; set; }

        public long Count { get; set; }
    }

    public class MonthlyExecutionSummary
    {
        public string Month { get; set; } = string.Empty;

        public long Success { get; set; }

        public long Error { get; set; }
    }

    public class DashboardData
    {
        public DashboardKpis Kpis { get; set; } = new();

        public List<MonthlyExecutionSummary> MonthlyExecutions { get; set; } = [];

        public List<QueueStatusSummary> QueueByStatus { get; set; } = [];
    }
}
